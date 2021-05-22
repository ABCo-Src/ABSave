﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Exceptions
{
    public class InvalidDocumentException : ABSaveException
    {
        public InvalidDocumentException(string msg) : base($"The document given is invalid. " + msg) { }
    }
}