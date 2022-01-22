﻿using System;

namespace PropertyBinder;

public class BindingException : Exception
{
    public BindingException()
    {
    }

    public BindingException(string message) : base(message)
    {
    }

    public BindingException(string message, Exception inner) : base(message, inner)
    {
    }
}