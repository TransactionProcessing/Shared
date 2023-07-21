using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Extensions
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public static class PropertyBuilderExtensions
    {
        [ExcludeFromCodeCoverage]
        public static PropertyBuilder DecimalPrecision(this PropertyBuilder propertyBuilder,
                                                       Int32 precision,
                                                       Int32 scale)
        {
            return propertyBuilder.HasColumnType($"decimal({precision},{scale})");
        }
    }
}
