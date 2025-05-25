using System.Text;
using System.Text.Json;

namespace Find;

class Program
{
    const string apiKey = "AIzaSyA8yIiVGbbQczBhnbRZrcRwnGbe9lsOWZg";
    const string cseId = "b29c9ab54a956443b";

    static async Task Main(string[] args)
    {
        Console.Write("Qual a cidade da busca? ");
        string cidade = Console.ReadLine()!;

        Console.Write("Qual o estado da busca? ");
        string estado = Console.ReadLine()!;

        Console.Write("\nDigite sua busca: ");
        string query = Console.ReadLine()!;

        await BuscarResultadosAsync(query, cidade, estado);
    }

    static async Task BuscarResultadosAsync(string query, string cidade, string estado)
    {
        using HttpClient client = new();

        int totalResultados = 100;
        int tamanhoPorPagina = 10;
        int inicio = 1;
        string cidadeEstado = $"{cidade} {estado}".Replace(" ", "_").ToLowerInvariant();
        string nomeArquivo = $"{cidadeEstado.ToUpper().Trim()}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string queryEditada = string.Empty;
        string palavrasASeremExcluidas = string.Empty;

        string pastaResultados = Path.Combine(AppContext.BaseDirectory, "Resultados");
        if (!Directory.Exists(pastaResultados))
            Directory.CreateDirectory(pastaResultados);

        string caminhoCompleto = Path.Combine(pastaResultados, nomeArquivo);

        using StreamWriter writer = new(caminhoCompleto, false, Encoding.UTF8);
        writer.WriteLine("Link;Título");

        #region Sites Excluídos
        List<string> excluidos =
        [
            "fatec",
            "instagram",
            "vaga",
            "linkedin",
            "facebook",
            "x.com",
            "twitter",
            "tiktok",
            "glassdor",
            "lovemonday",
            "curso",
            "fam",
            "senai",
            "edital",
            "prefeitura",
            "senac",
            "portal",
            "transparencia",
            "camara",
            "municipal",
            "gama",
            "gauchazh",
            "apple",
            "conecte5g",
            "tjsp",
            "amazon",
            "g1",
            "flixbus",
            "nubank",
            "portalamericana",
            "unicamp",
            "usp",
            "unesp",
            "secretaria",
            "agenciabrasil",
            "casamenta",
            "ufpe",
            "avt",
            "agricultura",
            "uber",
            "sind"
        ];
        #endregion

        for (int i = 0; i < totalResultados / tamanhoPorPagina; i++)
        {
            queryEditada = GeraQueryEditada(query, cidadeEstado, ref palavrasASeremExcluidas, excluidos);
            string url = @$"https://www.googleapis.com/customsearch/v1?q={Uri.EscapeDataString(queryEditada)}
                         &key={apiKey}&cx={cseId}&num={tamanhoPorPagina}&start={inicio}&hl=pt";

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                string content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erro {response.StatusCode}: {response.ReasonPhrase}");
                    Console.WriteLine(content);
                    break;
                }

                using JsonDocument doc = JsonDocument.Parse(content);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("items", out JsonElement items))
                {
                    foreach (JsonElement item in items.EnumerateArray())
                    {
                        string title = item.GetProperty("title").GetString()!;
                        string link = item.GetProperty("link").GetString()!;

                        Console.WriteLine($"Título: {title}");
                        Console.WriteLine($"Link: {link}\n");

                        string tituloSanitizado = title.Replace(";", " ").Replace("\n", " ").Replace("\r", " ");
                        writer.WriteLine($"{tituloSanitizado};{link}");
                    }
                }
                else
                {
                    Console.WriteLine("Nenhum resultado encontrado nesta página.");
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar página {i + 1}: {ex.Message}");
                break;
            }

            inicio += tamanhoPorPagina;
            await Task.Delay(250);
        }

        Console.WriteLine($"\nArquivo CSV gerado com sucesso: {nomeArquivo}");
    }

    private static string GeraQueryEditada(string query, string cidadeEstado, ref string palavrasASeremExcluidas, List<string> excluidos)
    {
        string queryEditada;
        foreach (string item in excluidos)
        {
            palavrasASeremExcluidas += $"-{item} ";
        }

        queryEditada = $"{query} {cidadeEstado.Replace("_", "/")} {palavrasASeremExcluidas}";
        return queryEditada;
    }
}
