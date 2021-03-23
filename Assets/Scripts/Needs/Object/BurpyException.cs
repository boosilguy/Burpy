using System.Collections;
using System.Collections.Generic;
using System;

public class BurpyException : Exception {
	public BurpyException() : base() { }
    public BurpyException(string message) : base(message) { }
    public BurpyException(string message, System.Exception inner) : base(message, inner) { }
}
