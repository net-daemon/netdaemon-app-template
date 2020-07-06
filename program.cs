using NetDaemon.Service;
using System.Threading.Tasks;

namespace Service
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            await Runner.Run(args).ConfigureAwait(false);
        }
    }
}