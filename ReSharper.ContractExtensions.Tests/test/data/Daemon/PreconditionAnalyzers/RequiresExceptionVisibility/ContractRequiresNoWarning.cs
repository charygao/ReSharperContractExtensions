#define CONTRACTS_FULL
using System.Diagnostics.Contracts;

public class A
{
  public void Foo(string s)
  {
    Contract.Requires<CustomException>(s != null);
  }
}

public class CustomException : System.ArgumentException
{
  public CustomException(string message, string paramName) : base(message, paramName) {}
}