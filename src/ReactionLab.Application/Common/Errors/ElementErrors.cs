namespace ReactionLab.Application.Common.Errors;

public static class ElementErrors
{
    public static readonly Error NotFound = new("Element.NotFound", "Element was not found");
    public static readonly Error SymbolNotFound = new("Element.SymbolNotFound", "Element with the specified symbol was not found");
    public static readonly Error DuplicateAtomicNumber = new("Element.DuplicateAtomicNumber", "An element with this atomic number already exists");
    public static readonly Error DuplicateSymbol = new("Element.DuplicateSymbol", "An element with this symbol already exists");
}