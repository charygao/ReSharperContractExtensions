using System;
using System.Diagnostics.Contracts;

class A
{
  private IEnumerable<Foo> Foo(string s)
  {
    Contract.Requires(s != null);
    return null;
  }
}