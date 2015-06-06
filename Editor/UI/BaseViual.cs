﻿using Orbital.Data;

namespace Orbital.UI
{
    public abstract class BaseViual
    {
        #region Methods

        public abstract void BeginDraw(IValueSource source, UIParameters parameters);

        public abstract void Draw(IValueSource source, UIParameters parameters);

        public abstract void EndDraw(IValueSource source, UIParameters parameters);

        #endregion
    }
}