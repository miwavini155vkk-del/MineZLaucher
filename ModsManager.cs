using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;

/// <summary>
/// Класс для управления скачиванием и проверкой модов
/// </summary>
public class ModsManager
{
    private readonly string _modsDirectory;
    private readonly HttpClient _httpClient;

    public ModsManager(string? modsPath = null)
    {
        // Используем переданный путь или создаём папку "mods" в рабочей директории
        _modsDirectory = modsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods");
        
        // Создаём папку если её нет
        if (!Directory.Exists(_modsDirectory))
        {
            Directory.CreateDirectory(_modsDirectory);
        }

        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Скачивает мод по URL в папку mods
    /// </summary>
    public async Task<bool> DownloadModAsync(string modUrl, string modFileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(modUrl) || string.IsNullOrWhiteSpace(modFileName))
            {
                throw new ArgumentException("URL и имя файла не могут быть пустыми");
            }

            string filePath = Path.Combine(_modsDirectory, modFileName);

            // Проверяем, не существует ли файл уже
            if (File.Exists(filePath))
            {
                Console.WriteLine($"Мод '{modFileName}' уже существует. Пропускаем скачивание.");
                return true;
            }

            Console.WriteLine($"Скачиваем мод: {modFileName}...");

            using (var response = await _httpClient.GetAsync(modUrl))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Ошибка при скачивании. Код ответа: {response.StatusCode}");
                }

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                {
                    using (var fileStream = File.Create(filePath))
                    {
                        await contentStream.CopyToAsync(fileStream);
                    }
                }
            }

            Console.WriteLine($"✓ Мод '{modFileName}' успешно скачан");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Ошибка при скачивании модов: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Скачивает несколько модов одновременно
    /// </summary>
    public async Task<List<bool>> DownloadModsAsync(Dictionary<string, string> mods)
    {
        var tasks = new List<Task<bool>>();

        foreach (var mod in mods)
        {
            tasks.Add(DownloadModAsync(mod.Value, mod.Key));
        }

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    /// <summary>
    /// Проверяет наличие и целостность модов в папке
    /// </summary>
    public List<string> CheckMods()
    {
        var modsInfo = new List<string>();

        try
        {
            if (!Directory.Exists(_modsDirectory))
            {
                modsInfo.Add($"⚠ Папка модов не существует: {_modsDirectory}");
                return modsInfo;
            }

            var modFiles = Directory.GetFiles(_modsDirectory, "*.jar")
                .Concat(Directory.GetFiles(_modsDirectory, "*.zip"))
                .ToList();

            if (modFiles.Count == 0)
            {
                modsInfo.Add("ℹ Модов не найдено в папке");
                return modsInfo;
            }

            modsInfo.Add($"✓ Найдено модов: {modFiles.Count}");

            foreach (var modFile in modFiles)
            {
                var fileInfo = new FileInfo(modFile);
                var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                modsInfo.Add($"  • {Path.GetFileName(modFile)} ({sizeInMB:F2} MB)");
            }

            return modsInfo;
        }
        catch (Exception ex)
        {
            modsInfo.Add($"✗ Ошибка при проверке модов: {ex.Message}");
            return modsInfo;
        }
    }

    /// <summary>
    /// Получает список всех модов в папке
    /// </summary>
    public List<string> GetModsList()
    {
        try
        {
            if (!Directory.Exists(_modsDirectory))
                return new List<string>();

            var modFiles = Directory.GetFiles(_modsDirectory, "*.jar")
                .Concat(Directory.GetFiles(_modsDirectory, "*.zip"))
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>()
                .ToList();

            return modFiles;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении списка модов: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Удаляет мод из папки
    /// </summary>
    public bool DeleteMod(string modFileName)
    {
        try
        {
            string filePath = Path.Combine(_modsDirectory, modFileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Мод '{modFileName}' не найден");
                return false;
            }

            File.Delete(filePath);
            Console.WriteLine($"✓ Мод '{modFileName}' удалён");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Ошибка при удалении модов: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Очищает всю папку модов
    /// </summary>
    public bool ClearAllMods()
    {
        try
        {
            if (!Directory.Exists(_modsDirectory))
                return true;

            Directory.Delete(_modsDirectory, true);
            Directory.CreateDirectory(_modsDirectory);
            Console.WriteLine("✓ Папка модов очищена");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Ошибка при очистке папки модов: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Получает путь к папке модов
    /// </summary>
    public string GetModsDirectoryPath()
    {
        return _modsDirectory;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
