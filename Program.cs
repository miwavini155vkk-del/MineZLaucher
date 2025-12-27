using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string minecraftPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        ".minecraft");
    
    private static readonly string expectedForgeVersion = "1.7.10-Forge10.13.4.1614-1.7.10";
    private static readonly string java8Path = @"C:\Program Files\Java\jre1.8.0_471\bin\java.exe";
    private static readonly string javaInstallerUrl = "https://github.com/miwavini155vkk-del/JRE8/releases/download/main/jre-8u471-windows-x64.1.exe";
    private static readonly string javaInstallerPath = Path.Combine(Path.GetTempPath(), "jre-8u471-windows-x64.1.exe");

    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== Minecraft Forge Launcher ===");
            
            // 1. Проверяем наличие Java 8
            if (!CheckJava8Exists())
            {
                Console.WriteLine("✗ Java 8 не найдена. Устанавливаем...");
                bool javaInstalled = await InstallJava8();
                if (!javaInstalled)
                {
                    Console.WriteLine("Не удалось установить Java 8.");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"✓ Java 8 найдена: {java8Path}");
            }
            
            // 2. Проверяем установлен ли Forge
            string? foundVersion = FindForgeVersion();
            
            if (!string.IsNullOrEmpty(foundVersion))
            {
                // 3. Если Forge найден - сразу запускаем
                Console.WriteLine($"✓ Forge найден: {foundVersion}");
                await LaunchMinecraft(foundVersion);
                return;
            }
            
            // 4. Если Forge не найден - устанавливаем
            Console.WriteLine("✗ Forge не найден. Начинаем установку...");
            
            CreateMinecraftDirectories();
            CreateLauncherProfilesFile();
            
            string? installerPath = await DownloadForgeInstaller();
            if (installerPath == null)
            {
                Console.WriteLine("Не удалось скачать установщик Forge.");
                return;
            }
            
            bool installed = await InstallForge(installerPath);
            if (!installed)
            {
                Console.WriteLine("Установка Forge не удалась.");
                return;
            }
            
            // 5. Проверяем установку после инсталляции
            foundVersion = FindForgeVersion();
            if (string.IsNullOrEmpty(foundVersion))
            {
                Console.WriteLine("Forge не установился. Проверьте вручную.");
                
                // Покажем все доступные версии
                Console.WriteLine("\nДоступные версии в папке versions:");
                string versionsPath = Path.Combine(minecraftPath, "versions");
                if (Directory.Exists(versionsPath))
                {
                    foreach (var dir in Directory.GetDirectories(versionsPath))
                    {
                        Console.WriteLine($"  - {Path.GetFileName(dir)}");
                    }
                }
                return;
            }
            
            Console.WriteLine($"✓ Forge установлен: {foundVersion}");
            
            // 6. Запускаем Minecraft
            await LaunchMinecraft(foundVersion);
            
            Console.WriteLine("=== Завершено ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    static bool CheckJava8Exists()
    {
        // Проверяем несколько возможных путей
        string[] possiblePaths = {
            @"C:\Program Files\Java\jre1.8.0_471\bin\java.exe",
            @"C:\Program Files (x86)\Java\jre1.8.0_471\bin\java.exe",
            @"C:\Program Files\Java\jre1.8.0\bin\java.exe",
            @"C:\Program Files (x86)\Java\jre1.8.0\bin\java.exe",
            @"C:\Program Files\Java\jdk1.8.0_471\bin\java.exe",
            @"C:\Program Files (x86)\Java\jdk1.8.0_471\bin\java.exe",
            @"C:\Program Files\Java\jdk1.8.0\bin\java.exe",
            @"C:\Program Files (x86)\Java\jdk1.8.0\bin\java.exe"
        };
        
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                Console.WriteLine($"Найдена Java по пути: {path}");
                return true;
            }
        }
        
        return false;
    }

    static async Task<bool> InstallJava8()
    {
        Console.WriteLine("1. Скачиваем установщик Java 8...");
        
        try
        {
            // Удаляем старый установщик если он есть
            if (File.Exists(javaInstallerPath))
            {
                try
                {
                    File.Delete(javaInstallerPath);
                    Console.WriteLine("   Удален старый установщик");
                    await Task.Delay(1000); // Даем время системе освободить файл
                }
                catch
                {
                    Console.WriteLine("   Не удалось удалить старый установщик, продолжаем...");
                }
            }
            
            // Скачиваем установщик
            Console.WriteLine($"   Скачиваем из: {javaInstallerUrl}");
            using var response = await httpClient.GetAsync(javaInstallerUrl);
            response.EnsureSuccessStatusCode();
            
            // Читаем данные в память, затем записываем в файл
            byte[] installerData = await response.Content.ReadAsByteArrayAsync();
            
            // Записываем в файл
            await File.WriteAllBytesAsync(javaInstallerPath, installerData);
            
            Console.WriteLine($"   ✓ Установщик сохранен: {javaInstallerPath}");
            
            // Даем время файловой системе
            await Task.Delay(1000);
            
            // Проверяем, что файл существует
            if (!File.Exists(javaInstallerPath))
            {
                Console.WriteLine("   ✗ Файл установщика не создался");
                return false;
            }
            
            // Запускаем установку
            Console.WriteLine("2. Устанавливаем Java 8 (тихая установка)...");
            
            var processInfo = new ProcessStartInfo
            {
                FileName = javaInstallerPath,
                Arguments = "/s REBOOT=Suppress", // Тихая установка без перезагрузки
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetTempPath() // Меняем рабочую директорию
            };

            Console.WriteLine($"   Запускаем: {processInfo.FileName} {processInfo.Arguments}");
            
            var process = Process.Start(processInfo);
            if (process == null)
            {
                Console.WriteLine("   ✗ Не удалось запустить процесс установки Java.");
                return false;
            }

            Console.WriteLine("   ⏳ Ожидаем установку Java (примерно 30-60 секунд)...");
            
            // Ждем завершения процесса
            bool exited = process.WaitForExit(180000); // 180 секунд таймаут
            if (!exited)
            {
                Console.WriteLine("   ⚠ Процесс установки Java не завершился за 3 минуты");
                process.Kill();
                await Task.Delay(2000);
            }
            
            // Даем системе время на завершение установки
            await Task.Delay(10000);
            
            // Удаляем установщик
            try
            {
                if (File.Exists(javaInstallerPath))
                {
                    for (int i = 0; i < 5; i++) // Пробуем несколько раз
                    {
                        try
                        {
                            File.Delete(javaInstallerPath);
                            Console.WriteLine("   ✓ Установщик удален");
                            break;
                        }
                        catch
                        {
                            await Task.Delay(1000);
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("   ⚠ Не удалось удалить установщик, продолжаем...");
            }
            
            // Проверяем, установилась ли Java
            Console.WriteLine("3. Проверяем установку Java...");
            await Task.Delay(5000); // Даем время системе обновиться
            
            if (CheckJava8Exists())
            {
                Console.WriteLine($"   ✓ Java 8 успешно установлена!");
                return true;
            }
            else
            {
                Console.WriteLine("   ✗ Java не найдена после установки");
                
                // Проверяем через командную строку
                try
                {
                    var javaCheckProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c java -version 2>&1",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    
                    javaCheckProcess.Start();
                    string output = await javaCheckProcess.StandardOutput.ReadToEndAsync();
                    javaCheckProcess.WaitForExit();
                    
                    if (output.Contains("1.8") || output.Contains("version 8"))
                    {
                        Console.WriteLine($"   ✓ Java 8 найдена через команду java -version");
                        return true;
                    }
                }
                catch
                {
                    // Игнорируем ошибки проверки
                }
                
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Ошибка установки Java: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Внутренняя ошибка: {ex.InnerException.Message}");
            }
            return false;
        }
    }

    static string? FindForgeVersion()
    {
        string versionsPath = Path.Combine(minecraftPath, "versions");
        
        if (!Directory.Exists(versionsPath))
        {
            Console.WriteLine($"Папка versions не найдена: {versionsPath}");
            return null;
        }
        
        // Ищем версию Forge 1.7.10
        var versionDirs = Directory.GetDirectories(versionsPath);
        
        // Сначала ищем точное имя Forge
        foreach (var dir in versionDirs)
        {
            string dirName = Path.GetFileName(dir);
            
            // Проверяем на точное соответствие ожидаемому имени Forge
            if (dirName.Equals(expectedForgeVersion, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"✓ Найдена версия Forge: {dirName}");
                return dirName;
            }
            
            // Проверяем содержит ли имя Forge и 1.7.10
            if (dirName.Contains("forge", StringComparison.OrdinalIgnoreCase) && 
                dirName.Contains("1.7.10"))
            {
                Console.WriteLine($"✓ Найдена версия Forge (содержит forge): {dirName}");
                return dirName;
            }
            
            // Проверяем если имя содержит версию Forge
            if (dirName.Contains("10.13.4.1614") && dirName.Contains("1.7.10"))
            {
                Console.WriteLine($"✓ Найдена версия Forge (содержит номер сборки): {dirName}");
                return dirName;
            }
        }
        
        // Если не нашли Forge, проверяем есть ли обычная 1.7.10
        // и проверяем не установлен ли Forge как библиотека
        foreach (var dir in versionDirs)
        {
            string dirName = Path.GetFileName(dir);
            
            if (dirName.Equals("1.7.10"))
            {
                // Проверяем, есть ли уже Forge в этой версии
                string jsonFile = Path.Combine(dir, $"{dirName}.json");
                if (File.Exists(jsonFile))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(jsonFile);
                        if (jsonContent.Contains("forge") || jsonContent.Contains("Forge") || 
                            jsonContent.Contains("10.13.4.1614"))
                        {
                            Console.WriteLine($"✓ Найдена версия 1.7.10 с Forge: {dirName}");
                            return dirName;
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки чтения
                    }
                }
            }
        }
        
        Console.WriteLine($"✗ Forge не найден в папке: {versionsPath}");
        return null;
    }

    static void CreateMinecraftDirectories()
    {
        Directory.CreateDirectory(Path.Combine(minecraftPath, "versions"));
        Directory.CreateDirectory(Path.Combine(minecraftPath, "libraries"));
        Directory.CreateDirectory(Path.Combine(minecraftPath, "mods"));
        Directory.CreateDirectory(Path.Combine(minecraftPath, "logs"));
        Directory.CreateDirectory(Path.Combine(minecraftPath, "saves"));
    }

    static void CreateLauncherProfilesFile()
    {
        string profilesPath = Path.Combine(minecraftPath, "launcher_profiles.json");
        if (!File.Exists(profilesPath))
        {
            var profiles = new
            {
                profiles = new { },
                settings = new { },
                version = 2
            };
            string json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(profilesPath, json);
        }
    }

    static async Task<string?> DownloadForgeInstaller()
    {
        Console.WriteLine("4. Скачиваем установщик Forge...");
        
        // Разные URL для скачивания Forge
        string[] forgeUrls = {
            "https://files.minecraftforge.net/maven/net/minecraftforge/forge/1.7.10-10.13.4.1614-1.7.10/forge-1.7.10-10.13.4.1614-1.7.10-installer.jar",
            "https://maven.minecraftforge.net/net/minecraftforge/forge/1.7.10-10.13.4.1614-1.7.10/forge-1.7.10-10.13.4.1614-1.7.10-installer.jar",
            "https://adfoc.us/serve/sitelinks/?id=271228&url=https://files.minecraftforge.net/maven/net/minecraftforge/forge/1.7.10-10.13.4.1614-1.7.10/forge-1.7.10-10.13.4.1614-1.7.10-installer.jar"
        };
        
        string installerPath = Path.Combine(minecraftPath, "forge-installer.jar");
        
        // Если установщик уже есть, не качаем повторно
        if (File.Exists(installerPath))
        {
            Console.WriteLine($"   ✓ Установщик уже существует");
            return installerPath;
        }
        
        foreach (string forgeUrl in forgeUrls)
        {
            try
            {
                Console.WriteLine($"   Пробуем URL: {forgeUrl}");
                using var response = await httpClient.GetAsync(forgeUrl);
                response.EnsureSuccessStatusCode();
                
                using var fileStream = File.Create(installerPath);
                await response.Content.CopyToAsync(fileStream);
                
                Console.WriteLine($"   ✓ Установщик сохранен");
                return installerPath;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"   ✗ Ошибка: {ex.Message}");
                continue;
            }
        }
        
        Console.WriteLine($"   ✗ Не удалось скачать установщик Forge ни с одного URL");
        return null;
    }

    static async Task<bool> InstallForge(string installerPath)
    {
        Console.WriteLine("5. Устанавливаем Forge...");
        
        // Проверяем Java еще раз
        if (!CheckJava8Exists())
        {
            Console.WriteLine($"   ✗ Java 8 не найдена!");
            return false;
        }
        
        try
        {
            // Получаем путь к Java
            string javaPath = GetJavaPath();
            if (string.IsNullOrEmpty(javaPath))
            {
                Console.WriteLine("   ✗ Не удалось найти путь к Java");
                return false;
            }
            
            Console.WriteLine($"   Используем Java: {javaPath}");
            
            // Устанавливаем Forge с автоматическим принятием лицензии
            Console.WriteLine("   Устанавливаем Forge (автоматический режим)...");
            
            // Создаем ответный файл для автоматической установки
            string installResponseFile = Path.Combine(Path.GetTempPath(), "forge_install.cfg");
            File.WriteAllText(installResponseFile, @"# Forge silent install configuration
# Generated by DayZ Launcher

# Install type (CLIENT, SERVER, EXTRACT)
INSTALLER_TYPE=CLIENT

# Install path (leave empty for default)
INSTALL_PATH=" + minecraftPath.Replace("\\", "\\\\") + @"

# Java home (leave empty for auto-detect)
JAVA_HOME=" + Path.GetDirectoryName(Path.GetDirectoryName(javaPath)).Replace("\\", "\\\\") + @"

# Other options
NOCONFIRM=true
NORESTART=true
");

            var processInfo = new ProcessStartInfo
            {
                FileName = javaPath,
                Arguments = $"-jar \"{installerPath}\" --installClient --installPath=\"{minecraftPath}\"",
                WorkingDirectory = minecraftPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Console.WriteLine($"   Команда: {processInfo.FileName} {processInfo.Arguments}");
            
            var process = Process.Start(processInfo);
            if (process == null)
            {
                Console.WriteLine("   ✗ Не удалось запустить процесс установки.");
                return false;
            }

            // Читаем вывод установщика
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            Console.WriteLine("   Вывод установщика Forge:");
            Console.WriteLine(output);
            
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("   Ошибки установщика:");
                Console.WriteLine(error);
            }
            
            // Удаляем конфиг файл
            if (File.Exists(installResponseFile))
            {
                File.Delete(installResponseFile);
            }
            
            if (process.ExitCode == 0)
            {
                Console.WriteLine("   ✓ Forge успешно установлен");
                
                // Удаляем установщик
                try
                {
                    if (File.Exists(installerPath))
                    {
                        File.Delete(installerPath);
                        Console.WriteLine("   ✓ Установщик удален");
                    }
                }
                catch
                {
                    Console.WriteLine("   ⚠ Не удалось удалить установщик");
                }
                
                return true;
            }
            else
            {
                Console.WriteLine($"   ⚠ Установщик завершился с кодом: {process.ExitCode}");
                Console.WriteLine("   Пробуем интерактивную установку...");
                return await InstallForgeInteractive(javaPath, installerPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Исключение при установке Forge: {ex.Message}");
            return false;
        }
    }

    static async Task<bool> InstallForgeInteractive(string javaPath, string installerPath)
    {
        try
        {
            // Пробуем интерактивную установку
            Console.WriteLine("   Запускаем интерактивный установщик Forge...");
            Console.WriteLine("   ПОЖАЛУЙСТА: В окне установщика выберите 'Install client'");
            Console.WriteLine("   и убедитесь, что путь установки: " + minecraftPath);
            
            var processInfo = new ProcessStartInfo
            {
                FileName = javaPath,
                Arguments = $"-jar \"{installerPath}\"",
                WorkingDirectory = minecraftPath,
                UseShellExecute = true,
                CreateNoWindow = false
            };

            var process = Process.Start(processInfo);
            if (process == null)
            {
                Console.WriteLine("   ✗ Не удалось запустить интерактивный установщик.");
                return false;
            }

            Console.WriteLine("   ⏳ Ожидаем завершение установки...");
            Console.WriteLine("   Подсказка: Если окно не появилось, проверьте панель задач.");
            
            // Ждем максимум 5 минут
            bool exited = process.WaitForExit(300000);
            if (!exited)
            {
                Console.WriteLine("   ⚠ Установщик не завершился за 5 минут");
                process.Kill();
            }
            
            // Даем время системе
            await Task.Delay(5000);
            
            // Удаляем установщик
            try
            {
                if (File.Exists(installerPath))
                {
                    File.Delete(installerPath);
                    Console.WriteLine("   ✓ Установщик удален");
                }
            }
            catch
            {
                Console.WriteLine("   ⚠ Не удалось удалить установщик");
            }
            
            // Проверяем установился ли Forge
            if (!string.IsNullOrEmpty(FindForgeVersion()))
            {
                Console.WriteLine("   ✓ Forge успешно установлен!");
                return true;
            }
            
            Console.WriteLine("   ⚠ Forge не найден после установки");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Ошибка интерактивной установки: {ex.Message}");
            return false;
        }
    }

    static string? GetJavaPath()
    {
        string[] possiblePaths = {
            @"C:\Program Files\Java\jre1.8.0_471\bin\java.exe",
            @"C:\Program Files (x86)\Java\jre1.8.0_471\bin\java.exe",
            @"C:\Program Files\Java\jre1.8.0\bin\java.exe",
            @"C:\Program Files (x86)\Java\jre1.8.0\bin\java.exe",
            @"C:\Program Files\Java\jdk1.8.0_471\bin\java.exe",
            @"C:\Program Files (x86)\Java\jdk1.8.0_471\bin\java.exe",
            @"C:\Program Files\Java\jdk1.8.0\bin\java.exe",
            @"C:\Program Files (x86)\Java\jdk1.8.0\bin\java.exe",
            "java.exe" // Попробуем найти в PATH
        };
        
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }
        
        // Проверяем через where
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "java",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            if (!string.IsNullOrEmpty(output))
            {
                string[] paths = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (string path in paths)
                {
                    string trimmedPath = path.Trim();
                    if (File.Exists(trimmedPath))
                    {
                        return trimmedPath;
                    }
                }
            }
        }
        catch
        {
            // Игнорируем ошибки
        }
        
        return null;
    }

    static async Task LaunchMinecraft(string versionName)
{
    Console.WriteLine($"6. Запускаем Minecraft {versionName}...");
    
    try
    {
        var path = new MinecraftPath(minecraftPath);
        var launcher = new MinecraftLauncher(path);
        
        // 1. Получаем список метаданных всех доступных версий
        var allVersions = await launcher.GetAllVersionsAsync();
        
        // 2. Ищем метаданные нужной версии (используем .Name)
        var versionMetadata = allVersions.FirstOrDefault(v => 
            v.Name.Equals(versionName, StringComparison.OrdinalIgnoreCase)) 
            ?? allVersions.FirstOrDefault(v => v.Name.Contains("forge", StringComparison.OrdinalIgnoreCase));

        if (versionMetadata == null)
        {
            Console.WriteLine("   ✗ Версия не найдена в списке доступных.");
            return;
        }

        // 3. Загружаем полноценный объект IVersion по имени из метаданных
        // Это решает ошибку CS0266 (преобразование типа)
        var version = await launcher.GetVersionAsync(versionMetadata.Name);
        
        Console.WriteLine($"   Подготовка версии: {versionMetadata.Name}");
        
        var session = MSession.CreateOfflineSession("ForgePlayer");
        var launchOption = new MLaunchOption
        {
            MaximumRamMb = 4096,
            MinimumRamMb = 1024,
            Session = session,
            StartVersion = version // Здесь должен быть объект IVersion
        };
        
        // 4. Создаем процесс. В новых версиях можно передавать либо имя строки, либо сам объект.
        // Используем сохраненное имя из метаданных.
        var process = await launcher.CreateProcessAsync(versionMetadata.Name, launchOption);
        
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        
        process.OutputDataReceived += (sender, e) => 
        { 
            if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[Minecraft] {e.Data}"); 
        };
        process.ErrorDataReceived += (sender, e) => 
        { 
            if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[Error] {e.Data}"); 
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        Console.WriteLine($"   ✓ Minecraft запущен! PID: {process.Id}");
        await process.WaitForExitAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ✗ Ошибка запуска: {ex.Message}");
    }
}
}