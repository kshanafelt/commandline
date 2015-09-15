// Copyright 2005-2015 Giacomo Stelluti Scala & Contributors. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace CommandLine.Tests.Fakes
{
    public enum Colors
    {
        Red,
        Green,
        Blue
    }

    class Simple_Options_With_Enum
    {
        [Option]
        public Colors Colors { get; set; }
    }

    class Simple_Valid_Values_With_Enum
    {
        [Option]
        [ValidValues(typeof(ValidColors))]
        public Colors Colors { get; set; }

    }

    class ValidColors : IEnumerable<Colors>
    {
        public IEnumerator<Colors> GetEnumerator()
        {
            yield return Colors.Red;
            yield return Colors.Blue;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
