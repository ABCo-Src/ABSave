// Code analysis messages that are suppressed.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "Suggestion to use 'new()' only works on the .NET 5 target, but this is a multi-target project and creating a #if for this defeats the whole point.", Scope = "module")]
