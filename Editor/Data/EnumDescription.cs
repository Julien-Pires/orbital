﻿using System.Linq;
using System.Collections.Generic;

namespace Orbital.Data
{
    internal sealed class EnumDescription : BaseDescription
    {
        #region Fields

        private readonly string[] _values;

        #endregion

        #region Properties

        public string[] Values
        {
            get { return _values; }
        }

        #endregion

        #region Constructors

        internal EnumDescription(string name, IEnumerable<string> value) : base(name)
        {
            _values = value.ToArray();
        }

        #endregion
    }
}