class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Exemple UDP ===");

        try
        {
            // Exécuter l'exemple UDP
            await UdpExample.RunExample();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'exécution de l'exemple UDP : {ex.Message}");
        }

        Console.WriteLine("\nAppuyez sur une touche pour continuer...");
        Console.ReadKey();
    }
}
