using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AutoCloneGenerator
{
	[Generator]
	public class AutoCloneGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			// Register a syntax receiver that will be created for each generation pass
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			// Retrieve the populated receiver
			if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
				return;

			// Add the attribute text
			/*var attributeText = @"
			[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false)]
			public sealed class AutoCloneAttribute : System.Attribute
			{
			}";
			context.AddSource("AutoCloneAttribute", SourceText.From(attributeText, Encoding.UTF8));*/

			// Get the compilation
			var compilation = context.Compilation;

			// Loop over the candidate classes
			foreach (var candidate in receiver.Candidates)
			{
				var model = compilation.GetSemanticModel(candidate.SyntaxTree);
				var symbol = model.GetDeclaredSymbol(candidate);

				// Check if the class has the AutoClone attribute
				if (symbol?.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == "Ambermoon.Data.Serialization.AutoCloneAttribute") == true)
				{
					// Generate the Clone method
					var classSource = GenerateCloneMethod(symbol);
					context.AddSource($"{symbol.Name}_AutoClone", SourceText.From(classSource, Encoding.UTF8));
					Debugger.Log(0, "AutoCloneGenerator", classSource);
				}
			}
		}

		private static IEnumerable<IPropertySymbol> GetAllProperties(INamedTypeSymbol typeSymbol)
		{
			var properties = new List<IPropertySymbol>();

			while (typeSymbol != null && typeSymbol.SpecialType != SpecialType.System_Object)
			{
				properties.AddRange(typeSymbol.GetMembers().OfType<IPropertySymbol>()
					.Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic && p.SetMethod != null));

				typeSymbol = typeSymbol.BaseType;
			}

			return properties;
		}

		private static string GenerateCloneMethod(INamedTypeSymbol classSymbol)
		{
			var className = classSymbol.Name;
			var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

			var properties = GetAllProperties(classSymbol);
			bool arrayFound = false;

			var cloneStatements = new StringBuilder();
			foreach (var property in properties)
			{
				if (property.Type.TypeKind == TypeKind.Array)
				{
					var arrayType = property.Type as IArrayTypeSymbol;
					if (arrayType.ElementType.TypeKind == TypeKind.Class)
					{
						var typeName = arrayType.ElementType.ToDisplayString();
						cloneStatements.AppendLine($"\t\t\t\t{property.Name} = this.{property.Name}.Select(e => new {typeName}(e)).ToArray(),");
					}
					else
					{
						arrayFound = true;
						cloneStatements.AppendLine($"\t\t\t\t{property.Name} = CloneArray(this.{property.Name}),");
					}
				}
				else if (property.Type.TypeKind == TypeKind.Class && property.Type.SpecialType != SpecialType.System_String)
				{
					cloneStatements.AppendLine($"\t\t\t\t{property.Name} = new(this.{property.Name}),");
				}
				else
				{
					cloneStatements.AppendLine($"\t\t\t\t{property.Name} = this.{property.Name},");
				}
			}

			var cloneArrayMethod = !arrayFound ? "" : @"
		private T[] CloneArray<T>(T[] array) where T : struct
		{
			if (array == null)
				return null;

			var clone = new T[array.Length];

			for (int i = 0; i < array.Length; ++i)
			{
				clone[i] = array[i];
			}

			return clone;
		}
";

			return $@"
using System.Linq;
using Ambermoon.Data.Serialization;

namespace {namespaceName}
{{
	public partial class {className} : IAutoClone<{className}>
	{{{cloneArrayMethod}
		public partial {className} DeepClone()
		{{
			return new {className}()
			{{
				// Copy all properties
{cloneStatements.ToString().TrimEnd(',', '\r', '\n')}
			}};
		}}
	}}
}}
";
		}

		class SyntaxReceiver : ISyntaxReceiver
		{
			public List<ClassDeclarationSyntax> Candidates { get; } = new List<ClassDeclarationSyntax>();

			// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				// Any class with the attribute
				if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
					&& classDeclarationSyntax.AttributeLists.Count > 0)
				{
					Candidates.Add(classDeclarationSyntax);
				}
			}
		}
	}
}
