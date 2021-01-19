using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Helpers
{
    /// <summary>
    /// A value that is either the left, or the right.
    /// </summary>
    public struct Either<TLeft, TRight>
        where TLeft : class
    {
        public TLeft Left;
        public TRight Right;

        public Either(TLeft left)
        {
            Left = left;
            Right = default;
        }

        public Either(TRight right)
        {
            Left = null;
            Right = right;
        }
    }
}
