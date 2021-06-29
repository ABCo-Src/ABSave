namespace ABCo.ABSave.Exceptions
{
    public class UnrecognizedCollectionException : ABSaveException
    {
        public UnrecognizedCollectionException() : base("ABSave does not support the collection given. Keep in mind that ABSave cannot serialize plain IEnumerables.") { }
    }
}
