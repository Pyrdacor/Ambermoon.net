using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Numerics;

namespace Ambermoon.Data.GameDataRepository.Util
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class AdvancedOnlyAttribute : System.Attribute
    {
    }
}
