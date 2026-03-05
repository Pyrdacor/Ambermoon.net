// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "I like x.ToList() etc as it is more expressive than [.. x]")]
[assembly: SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "I often like it more to specify properties after object creation.")]
