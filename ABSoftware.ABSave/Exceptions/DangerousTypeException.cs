namespace ABCo.ABSave.Exceptions
{
    public class DangerousTypeException : ABSaveException
    {
        public DangerousTypeException(string details) : base($"ABSave detected {details}. This is extremely dangerous with inheritance enabled if the ABSave is from an untrusted source, as it may allow an attacker to create an instance of any type they want, such as 'Socket' making network connections. See the configuration section of the documentation for more information.") { }
    }
}
