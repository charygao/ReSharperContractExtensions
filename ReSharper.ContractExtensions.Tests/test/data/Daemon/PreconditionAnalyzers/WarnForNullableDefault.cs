#define CONTRACTS_FULL
using System.Diagnostics.Contracts;

class A
{
  public void Foo(string s = null)
  {
    Contract.Requires(s != null);
  }
}