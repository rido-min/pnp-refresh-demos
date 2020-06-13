using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

namespace PnPConvention.Tests
{
  public class TestLogging
  {
    private static ILoggerFactory _Factory = null;

    public static void ConfigureLogger(ILoggerFactory factory)
    {
      factory.AddProvider(new DebugLoggerProvider());
    }

    public static ILoggerFactory LoggerFactory
    {
      get
      {
        if (_Factory == null)
        {
          _Factory = new LoggerFactory();
          ConfigureLogger(_Factory);
        }
        return _Factory;
      }
      set { _Factory = value; }
    }
    public static ILogger CreateLogger() => LoggerFactory.CreateLogger("tests");
  }
}
