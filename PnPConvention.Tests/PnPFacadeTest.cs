using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace PnPConvention.Tests
{
  public class PnPFacadeTest
  {
    [Fact]
    public void ReadProp()
    {
      var facade = PnPFacade.CreateFromConnectionStringAndModelId("", "");
      
    }
  }
}
