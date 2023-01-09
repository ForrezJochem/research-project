[assembly: Rage.Attributes.Plugin("GtaTrainingHelper", Description = "Setups game for training.", Author = "Jochem Forrez")]
namespace GtaTrainingHelper
{
    using Rage;
    public class Program
    {
        private static void Main()
        {
            Game.Console.Print("GtaTrainingHelper Starting.");
        }
    }
}
