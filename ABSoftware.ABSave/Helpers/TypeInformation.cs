using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Helpers
{
    /// <summary>
    /// Information about a given member's type, including what type of data it actually is, and what type of data the object it's contained in says it is. (These will be different for things such as inheritance or generics)
    /// </summary>
    public struct TypeInformation
    {
        /// <summary>
        /// The actual type of the data that's there.
        /// </summary>
        public Type ActualType;

        /// <summary>
        /// The actual type code of the data that's there.
        /// </summary>
        public TypeCode ActualTypeCode;

        /// <summary>
        /// The type that's specified by the containing object or array. This may be different from the <see cref="ActualType"/> for things such as inheritance or generics.
        /// </summary>
        public Type SpecifiedType;

        /// <summary>
        /// The type code that's specified by the containing object or array. This may be different from the <see cref="ActualTypeCode"/> for things such as inheritance or generics.
        /// </summary>
        public TypeCode SpecifiedTypeCode;

        public TypeInformation(Type actualType, TypeCode actualTypeCode)
        {
            ActualType = actualType;
            ActualTypeCode = actualTypeCode;
            SpecifiedType = null;
            SpecifiedTypeCode = TypeCode.Empty;
        }

        public TypeInformation(Type actualType, TypeCode actualTypeCode, Type specifiedType, TypeCode specifiedTypeCode) : this(actualType, actualTypeCode)
        {
            SpecifiedType = specifiedType;
            SpecifiedTypeCode = specifiedTypeCode;
        }
    }
}