// <copyright file="CharExtensionsTest.cs" company="TVA">No copyright is claimed pursuant to 17 USC § 105.  All Other Rights Reserved.</copyright>

using System;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TVA;
using System.Collections.Generic;

namespace TVA
{
    [TestClass]
    [PexClass(typeof(CharExtensions))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    public partial class CharExtensionsTest
    {
        [PexMethod]
        public bool IsAny(char value, IEnumerable<char> testChars)
        {
            bool result = CharExtensions.IsAny(value, testChars);
            return result;
            // TODO: add assertions to method CharExtensionsTest.IsAny(Char, IEnumerable`1<Char>)
        }
        [PexMethod]
        public bool IsInRange(
            char value,
            char startOfRange,
            char endOfRange
        )
        {
            bool result = CharExtensions.IsInRange(value, startOfRange, endOfRange);
            return result;
            // TODO: add assertions to method CharExtensionsTest.IsInRange(Char, Char, Char)
        }
        [PexMethod]
        public bool IsNumeric(char value)
        {
            bool result = CharExtensions.IsNumeric(value);
            return result;
            // TODO: add assertions to method CharExtensionsTest.IsNumeric(Char)
        }
        [PexMethod]
        public bool IsWordTerminator(char value)
        {
            bool result = CharExtensions.IsWordTerminator(value);
            return result;
            // TODO: add assertions to method CharExtensionsTest.IsWordTerminator(Char)
        }
        [PexMethod]
        public string RegexEncode(char item)
        {
            string result = CharExtensions.RegexEncode(item);
            return result;
            // TODO: add assertions to method CharExtensionsTest.RegexEncode(Char)
        }
    }
}
