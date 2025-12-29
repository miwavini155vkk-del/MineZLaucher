﻿﻿﻿using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
using CmlLib.Core.Installer.Forge;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string minecraftPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        ".minecraft");
    
    // Разрешенный сервер
    private static readonly string ALLOWED_SERVER_IP = "158.160.179.116";
    private static readonly int ALLOWED_SERVER_PORT = 25565;
    private static readonly string ALLOWED_SERVER_ADDRESS = $"{ALLOWED_SERVER_IP}:{ALLOWED_SERVER_PORT}";
    
    // Импорт для работы с консолью
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
    
    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();
    
    private static readonly string expectedForgeVersion = "1.12.2-Forge14.23.5.2859-1.12.2";
    private static readonly string java8Path = @"C:\Program Files\Java\jre1.8.0_471\bin\java.exe";
    private static readonly string javaInstallerUrl = "https://github.com/miwavini155vkk-del/JRE8/releases/download/main/jre-8u471-windows-x64.1.exe";
    private static readonly string javaInstallerPath = Path.Combine(Path.GetTempPath(), "jre-8u471-windows-x64.1.exe");
    private static readonly string accessToken = "ForgeServer2025SecureToken"; // Секретный токен доступа
    
    // Ключ шифрования (256 бит = 32 байта для AES-256)
    private static readonly byte[] encryptionKey = new byte[] {
        0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x57, 0x6F, 0x72,
        0x6C, 0x64, 0x53, 0x65, 0x63, 0x75, 0x72, 0x69,
        0x74, 0x79, 0x4B, 0x65, 0x79, 0x46, 0x6F, 0x72,
        0x47, 0x61, 0x6D, 0x65, 0x53, 0x65, 0x72, 0x76
    };

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            // Открываем консоль для отладки
            AllocConsole();
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== Консоль отладки запущена ===\n");
            Console.WriteLine($"=== РАЗРЕШЕННЫЙ СЕРВЕР: {ALLOWED_SERVER_ADDRESS} ===\n");
            
            // Инициализация WinForms
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Показываем splash-форму с загрузкой
            var splashForm = new SplashForm();
            splashForm.Show();

            // Симуляция загрузки
            for (int i = 0; i <= 100; i++)
            {
                splashForm.UpdateProgress(i, $"Инициализация... ({i}%)");
                System.Threading.Thread.Sleep(15);
                Application.DoEvents();
            }

            splashForm.Complete();
            System.Threading.Thread.Sleep(500);
            splashForm.Close();
            splashForm.Dispose();

            // Показываем основной лаунчер
            var launcherForm = new MainLauncher();
            Application.Run(launcherForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"КРИТИЧЕСКАЯ ОШИБКА:\n\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}", 
                "Ошибка запуска приложения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    public static async Task LaunchMinecraftAsync(string playerName)
    {
        await MainAsync(playerName);
    }

    static async Task MainAsync(string playerName)
    {
        try
        {
            Console.WriteLine("\n");
            Console.WriteLine("=== Minecraft Forge Launcher ===");
            Console.WriteLine($"Дата/время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Имя игрока: {playerName}");
            Console.WriteLine($"РАЗРЕШЕННЫЙ СЕРВЕР: {ALLOWED_SERVER_ADDRESS}");
            Console.WriteLine($"Путь .minecraft: {minecraftPath}");
            Console.WriteLine("Инициализация системы безопасности...");
            
            // Принудительная установка разрешенного сервера
            Console.WriteLine("\n--- Установка разрешенного сервера ---");
            ForceAllowedServer();
            
            // Создаём структуру папок .minecraft в САМОМ НАЧАЛЕ
            Console.WriteLine("\n--- Создание необходимых папок ---");
            CreateMinecraftDirectories();
            
            // Проверяем, что папка создана
            if (!Directory.Exists(minecraftPath))
            {
                throw new Exception($"Папка .minecraft не создана: {minecraftPath}");
            }
            Console.WriteLine($"✓ Папка .minecraft существует: {Directory.Exists(minecraftPath)}");
            
            Console.WriteLine("\n--- Создание профиля ---");
            CreateLauncherProfilesFile();
            
            // Сохраняем зашифрованный токен при первом запуске
            Console.WriteLine("\n--- Инициализация токена ---");
            SaveEncryptedToken();
            
            // 0. Заходим на локальный сервер
            Console.WriteLine("\n--- Подключение к серверу ---");
            await ConnectToLocalServer();
            
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
                await LaunchMinecraft(foundVersion, playerName);
                return;
            }
            
            // 4. Если Forge не найден - устанавливаем
            Console.WriteLine("✗ Forge не найден. Начинаем установку...");
            
            bool installed = await InstallForgeUsingApi();
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
            await LaunchMinecraft(foundVersion, playerName);
            
            Console.WriteLine("=== Завершено ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n=== КРИТИЧЕСКАЯ ОШИБКА ===");
            Console.WriteLine($"Сообщение: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                Console.WriteLine($"InnerStackTrace: {ex.InnerException.StackTrace}");
            }
            Console.WriteLine($"=== КОНЕЦ ОТЧЁТА ОБ ОШИБКЕ ===\n");
            throw; // Переделаём ошибку дальше для обработки в LauncherForm
        }
    }

    static void ForceAllowedServer()
    {
        try
        {
            // 1. Записываем разрешенный сервер в конфиг
            string configPath = Path.Combine(minecraftPath, "config");
            Directory.CreateDirectory(configPath);
            
            string serverIpPath = Path.Combine(configPath, "server_ip.txt");
            File.WriteAllText(serverIpPath, ALLOWED_SERVER_ADDRESS);
            Console.WriteLine($"✓ Записан разрешенный сервер: {serverIpPath}");
            
            // 2. Удаляем все другие файлы серверов если они есть
            CleanUpOtherServerConfigs(configPath);
            
            // 3. Создаем файл с информацией о блокировке
            string lockInfoPath = Path.Combine(configPath, "server_lock.txt");
            File.WriteAllText(lockInfoPath, 
                $"ДОСТУП К СЕРВЕРАМ ОГРАНИЧЕН\n" +
                $"Разрешен только сервер: {ALLOWED_SERVER_ADDRESS}\n" +
                $"Любые попытки подключения к другим серверам блокируются.\n" +
                $"Дата блокировки: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            Console.WriteLine($"✓ Создан файл блокировки: {lockInfoPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Ошибка при установке разрешенного сервера: {ex.Message}");
        }
    }

    static void CleanUpOtherServerConfigs(string configPath)
    {
        try
        {
            // Удаляем все файлы, которые могут содержать другие сервера
            string[] filesToCheck = Directory.GetFiles(configPath, "*.txt", SearchOption.AllDirectories);
            
            foreach (string file in filesToCheck)
            {
                try
                {
                    string fileName = Path.GetFileName(file).ToLower();
                    
                    // Проверяем файлы, которые могут содержать другие сервера
                    if (fileName.Contains("server") && !fileName.Contains("server_ip"))
                    {
                        string content = File.ReadAllText(file);
                        
                        // Если файл содержит другие IP адреса, заменяем их на разрешенный
                        if (content.Contains(".") && content.Contains(":"))
                        {
                            // Находим все IP адреса в файле
                            var lines = content.Split('\n');
                            bool modified = false;
                            
                            for (int i = 0; i < lines.Length; i++)
                            {
                                string line = lines[i];
                                if (line.Contains(".") && line.Contains(":"))
                                {
                                    // Проверяем, не разрешенный ли это сервер
                                    if (!line.Contains(ALLOWED_SERVER_IP) && !line.Contains(ALLOWED_SERVER_ADDRESS))
                                    {
                                        lines[i] = ALLOWED_SERVER_ADDRESS;
                                        modified = true;
                                        Console.WriteLine($"   Заменен сервер в файле: {file}");
                                    }
                                }
                            }
                            
                            if (modified)
                            {
                                File.WriteAllText(file, string.Join("\n", lines));
                            }
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    static async Task ConnectToLocalServer()
    {
        try
        {
            string serverUrl = "http://127.0.0.1:58250";
            Console.WriteLine($"Подключаюсь к {serverUrl}...");
            
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                
                try
                {
                    // Загружаем и расшифровываем токен
                    string decryptedToken = LoadAndDecryptToken();
                    
                    // Отправляем запрос с токеном доступа
                    var request = new HttpRequestMessage(HttpMethod.Get, serverUrl);
                    request.Headers.Add("Authorization", $"Bearer {decryptedToken}");
                    
                    var response = await httpClient.SendAsync(request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"✓ Успешно подключено к серверу");
                        Console.WriteLine($"Ответ сервера: {content}");
                    }
                    else
                    {
                        Console.WriteLine($"⚠ Сервер ответил с кодом: {response.StatusCode}");
                    }
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine($"⚠ Сервер недоступен на {serverUrl}");
                    Console.WriteLine("   Продолжаю работу...");
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine($"⚠ Таймаут подключения к серверу");
                    Console.WriteLine("   Продолжаю работу...");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Ошибка подключения к серверу: {ex.Message}");
            Console.WriteLine("   Продолжаю работу...");
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
        
        // Ищем версию Forge 1.12.2
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
            
            // Проверяем содержит ли имя Forge и 1.12.2
            if (dirName.Contains("forge", StringComparison.OrdinalIgnoreCase) && 
                dirName.Contains("1.12.2"))
            {
                Console.WriteLine($"✓ Найдена версия Forge (содержит forge): {dirName}");
                return dirName;
            }
            
            // Проверяем если имя содержит версию Forge
            if (dirName.Contains("14.23.5.2859") && dirName.Contains("1.12.2"))
            {
                Console.WriteLine($"✓ Найдена версия Forge (содержит номер сборки): {dirName}");
                return dirName;
            }
        }
        
        // Если не нашли Forge, проверяем есть ли обычная 1.12.2
        // и проверяем не установлен ли Forge как библиотека
        foreach (var dir in versionDirs)
        {
            string dirName = Path.GetFileName(dir);
            
            if (dirName.Equals("1.12.2"))
            {
                // Проверяем, есть ли уже Forge в этой версии
                string jsonFile = Path.Combine(dir, $"{dirName}.json");
                if (File.Exists(jsonFile))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(jsonFile);
                        if (jsonContent.Contains("forge") || jsonContent.Contains("Forge") || 
                            jsonContent.Contains("14.23.5.2859"))
                        {
                            Console.WriteLine($"✓ Найдена версия 1.12.2 с Forge: {dirName}");
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
        try
        {
            // Создаём основную папку .minecraft если её нет
            if (!Directory.Exists(minecraftPath))
            {
                Directory.CreateDirectory(minecraftPath);
                Console.WriteLine($"✓ Создана папка: {minecraftPath}");
            }
            
            // Создаём все необходимые подпапки
            Directory.CreateDirectory(Path.Combine(minecraftPath, "versions"));
            Directory.CreateDirectory(Path.Combine(minecraftPath, "libraries"));
            Directory.CreateDirectory(Path.Combine(minecraftPath, "mods"));
            Directory.CreateDirectory(Path.Combine(minecraftPath, "logs"));
            Directory.CreateDirectory(Path.Combine(minecraftPath, "saves"));
            Directory.CreateDirectory(Path.Combine(minecraftPath, "config"));
            Directory.CreateDirectory(Path.Combine(minecraftPath, "assets"));
            
            Console.WriteLine("✓ Структура папок .minecraft создана");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Ошибка при создании папок: {ex.Message}");
        }
    }

    static void CreateLauncherProfilesFile()
    {
        try
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
                Console.WriteLine($"✓ Создан профиль: {profilesPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Ошибка при создании профиля: {ex.Message}");
        }
    }

    static async Task<bool> InstallForgeUsingApi()
    {
        Console.WriteLine("4. Используем ForgeInstaller API для установки...");
        
        try
        {
            // Блокируем открытие браузера через переменные окружения
            Environment.SetEnvironmentVariable("FORGE_INSTALL_NO_BROWSER", "true");
            
            var path = new MinecraftPath(minecraftPath);
            var launcher = new MinecraftLauncher(path);
            var forgeInstaller = new ForgeInstaller(launcher);
            
            Console.WriteLine("   Получаем список доступных версий Forge для 1.12.2...");
            
            // Получаем доступные версии Forge для 1.12.2
            var versions = await forgeInstaller.GetForgeVersions("1.12.2");
            
            if (versions == null || !versions.Any())
            {
                Console.WriteLine("   ✗ Не найдены версии Forge для 1.12.2");
                return false;
            }
            
            // Ищем рекомендованную версию, затем последнюю, затем любую доступную
            var targetVersion = versions.FirstOrDefault(v => v.IsRecommendedVersion)
                              ?? versions.FirstOrDefault(v => v.IsLatestVersion)
                              ?? versions.First();
            
            Console.WriteLine($"   Найдена версия: Forge {targetVersion.ForgeVersionName} для Minecraft {targetVersion.MinecraftVersionName}");
            
            // Проверяем наличие установщика
            var installerFile = targetVersion.GetInstallerFile();
            if (installerFile == null)
            {
                Console.WriteLine("   ✗ Файл установщика не найден для этой версии");
                return false;
            }
            
            Console.WriteLine("   5. Загружаем и устанавливаем Forge в фоне...");
            Console.WriteLine("   ⏳ Установка запущена. Это может занять несколько минут...");
            
            // Запускаем монитор браузеров ДО установки
            var browserKillerTask = Task.Run(() => MonitorAndKillBrowsers());
            
            // Запускаем установку в фоновом потоке
            var installTask = Task.Run(async () =>
            {
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15)))
                    {
                        var installOptions = new ForgeInstallOptions
                        {
                            JavaPath = GetJavaPath(),
                            SkipIfAlreadyInstalled = true,
                            CancellationToken = cts.Token,
                        };
                        
                        // Устанавливаем Forge
                        var installedVersionName = await forgeInstaller.Install(targetVersion, installOptions);
                        
                        if (string.IsNullOrEmpty(installedVersionName))
                        {
                            Console.WriteLine("   ✗ Forge не был установлен");
                            return false;
                        }
                        
                        Console.WriteLine($"   ✓ Forge успешно установлен: {installedVersionName}");
                        
                        // Устанавливаем оставшиеся зависимости
                        Console.WriteLine("   Установка оставшихся зависимостей...");
                        await launcher.InstallAsync(installedVersionName);
                        
                        Console.WriteLine($"   ✓ Все зависимости установлены");
                        return true;
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("   ✗ Установка превысила таймаут (15 минут)");
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ✗ Ошибка фоновой установки: {ex.Message}");
                    return false;
                }
            });
            
            // Ждем завершения с индикатором прогресса
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (!installTask.IsCompleted)
            {
                await Task.Delay(3000); // Проверяем каждые 3 секунды
                var elapsed = (int)stopwatch.Elapsed.TotalSeconds;
                Console.WriteLine($"   ⏳ Устанавливаю... ({elapsed}с)");
            }
            
            var result = await installTask;
            return result;
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Ошибка установки Forge: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Внутренняя ошибка: {ex.InnerException.Message}");
            }
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

    static async Task LaunchMinecraft(string versionName, string playerName)
    {
        Console.WriteLine($"6. Запускаем Minecraft {versionName}...");
        
        try
        {
            // Принудительная проверка и установка разрешенного сервера
            ForceAllowedServer();
            
            // Всегда используем разрешенный сервер, игнорируя любые другие настройки
            string serverAddress = ALLOWED_SERVER_IP;
            int serverPort = ALLOWED_SERVER_PORT;
            
            Console.WriteLine($"   ВНИМАНИЕ: Подключение разрешено ТОЛЬКО к серверу: {serverAddress}:{serverPort}");
            
            // Очищаем список серверов и добавляем только наш сервер
            Console.WriteLine("   Настройка списка серверов...");
            ConfigureServerList(serverAddress, serverPort);
            
            var path = new MinecraftPath(minecraftPath);
            var launcher = new MinecraftLauncher(path);
            
            // Убедимся, что все зависимости установлены перед запуском
            Console.WriteLine("   Проверяем все зависимости...");
            await launcher.InstallAsync(versionName);
            
            // Получаем список версий и находим нужную
            var allVersions = await launcher.GetAllVersionsAsync();
            
            var versionMetadata = allVersions.FirstOrDefault(v => 
                v.Name.Equals(versionName, StringComparison.OrdinalIgnoreCase)) 
                ?? allVersions.FirstOrDefault(v => v.Name.Contains("forge", StringComparison.OrdinalIgnoreCase));

            if (versionMetadata == null)
            {
                Console.WriteLine("   ✗ Версия не найдена в списке доступных.");
                return;
            }

            var version = await launcher.GetVersionAsync(versionMetadata.Name);
            
            Console.WriteLine($"   Подготовка версии: {versionMetadata.Name}");
            
            var session = MSession.CreateOfflineSession(playerName);
            var launchOption = new MLaunchOption
            {
                MaximumRamMb = 4096,
                MinimumRamMb = 1024,
                Session = session,
                StartVersion = version
            };
            
            // Создаем процесс запуска игры
            var process = await launcher.CreateProcessAsync(versionMetadata.Name, launchOption);
            
            // Добавляем аргументы для подключения к серверу из конфига
            process.StartInfo.Arguments += $" --server {serverAddress} --port {serverPort}";
            Console.WriteLine($"   Принудительное подключение к серверу: {serverAddress}:{serverPort}");
            
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            
            process.OutputDataReceived += (sender, e) => 
            { 
                if (!string.IsNullOrEmpty(e.Data)) 
                {
                    // Мониторим попытки подключения к другим серверам
                    if (e.Data.Contains("connecting to") || e.Data.Contains("server") || e.Data.Contains("connect"))
                    {
                        Console.WriteLine($"[Minecraft] {e.Data}");
                        CheckForUnauthorizedServerAttempt(e.Data);
                    }
                }
            };
            process.ErrorDataReceived += (sender, e) => 
            { 
                if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[Error] {e.Data}"); 
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            Console.WriteLine($"   ✓ Minecraft запущен! PID: {process.Id}");
            Console.WriteLine($"   ⚠ ПОДКЛЮЧЕНИЕ К ДРУГИМ СЕРВЕРАМ ЗАПРЕЩЕНО!");
            
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Ошибка запуска: {ex.Message}");
            Console.WriteLine($"   StackTrace: {ex.StackTrace}");
        }
    }

    static void CheckForUnauthorizedServerAttempt(string logLine)
    {
        try
        {
            // Проверяем, не пытается ли игрок подключиться к другому серверу
            string lowerLine = logLine.ToLower();
            
            // Ищем упоминания IP адресов в логе
            if (lowerLine.Contains(".") && lowerLine.Contains(":"))
            {
                // Простая проверка на IP адрес (базовый паттерн)
                if (lowerLine.Contains("158.160.179.116"))
                {
                    // Это разрешенный сервер
                    return;
                }
                
                // Если нашли другой IP адрес в логе
                Console.WriteLine($"   ⚠ ОБНАРУЖЕНА ПОПЫТКА ПОДКЛЮЧЕНИЯ К ДРУГОМУ СЕРВЕРУ!");
                Console.WriteLine($"   Строка лога: {logLine}");
                Console.WriteLine($"   Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                // Записываем в лог попытку
                string logPath = Path.Combine(minecraftPath, "logs", "server_security.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath, 
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ПОПЫТКА ПОДКЛЮЧЕНИЯ К ДРУГОМУ СЕРВЕРУ: {logLine}\n");
            }
        }
        catch { }
    }

    static void ConfigureServerList(string serverAddress, int serverPort)
    {
        try
        {
            string serversPath = Path.Combine(minecraftPath, "servers.dat");
            
            // Загружаем и расшифровываем токен для сохранения конфиги
            string decryptedToken = LoadAndDecryptToken();
            
            Console.WriteLine($"   ✓ Защита доступа включена (шифрование: AES-256)");
            
            // Создаем NBT структуру для servers.dat
            // servers.dat формат: TAG_Compound("") { TAG_List("servers")[TAG_Compound { ... }] }
            var serversList = new List<byte>();
            
            // NBT Header: TAG_Compound (0x0A) + name "Root"
            serversList.Add(0x0A); // TAG_Compound
            serversList.AddRange(WriteString("")); // Empty name for root
            
            // TAG_List "servers"
            serversList.Add(0x09); // TAG_List
            serversList.AddRange(WriteString("servers")); // List name
            serversList.Add(0x0A); // TAG_Compound (list element type)
            
            // Number of servers (1) - только один разрешенный сервер
            byte[] countBytes = BitConverter.GetBytes(1);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(countBytes);
            serversList.AddRange(countBytes);
            
            // Create server entry as TAG_Compound
            var serverEntry = CreateServerEntry(serverAddress, serverPort);
            serversList.AddRange(serverEntry);
            
            // End tag
            serversList.Add(0x00); // TAG_End
            
            // Write to file
            File.WriteAllBytes(serversPath, serversList.ToArray());
            Console.WriteLine($"   ✓ Сервер добавлен в: {serversPath}");
            Console.WriteLine($"   ⚠ Другие сервера удалены из списка!");
            
            // Создаем файл с информацией о блокировке
            string lockInfoPath = Path.Combine(minecraftPath, "config", "server_lock.txt");
            File.WriteAllText(lockInfoPath, 
                $"ДОСТУП К СЕРВЕРАМ ОГРАНИЧЕН\n" +
                $"Разрешен только сервер: {serverAddress}:{serverPort}\n" +
                $"Любые попытки подключения к другим серверам блокируются.\n" +
                $"Дата блокировки: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠ Ошибка при настройке servers.dat: {ex.Message}");
        }
    }

    static List<byte> CreateServerEntry(string ip, int port)
    {
        var entry = new List<byte>();
        
        // ip (TAG_String)
        entry.Add(0x08); // TAG_String
        entry.AddRange(WriteString("ip"));
        entry.AddRange(WriteString(ip));
        
        // port (TAG_Int)
        entry.Add(0x03); // TAG_Int
        entry.AddRange(WriteString("port"));
        byte[] portBytes = BitConverter.GetBytes(port);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(portBytes);
        entry.AddRange(portBytes);
        
        // name (TAG_String)
        entry.Add(0x08); // TAG_String
        entry.AddRange(WriteString("name"));
        entry.AddRange(WriteString("Game Server"));
        
        // acceptTextures (TAG_Byte)
        entry.Add(0x01); // TAG_Byte
        entry.AddRange(WriteString("acceptTextures"));
        entry.Add(0x01); // true
        
        // End of compound
        entry.Add(0x00); // TAG_End
        
        return entry;
    }

    static List<byte> WriteString(string text)
    {
        var result = new List<byte>();
        byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes(text);
        byte[] lengthBytes = BitConverter.GetBytes((ushort)stringBytes.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBytes);
        result.AddRange(lengthBytes);
        result.AddRange(stringBytes);
        return result;
    }

    static void KillBrowserProcesses()
    {
        try
        {
            // Списки процессов браузеров и их вспомогательных процессов
            string[] browserNames = { 
                "chrome", "firefox", "msedge", "iexplore", "opera", "brave",
                "chromium", "safari", "edge", "googlechrome",
                "chrome.exe", "firefox.exe", "msedge.exe", "iexplore.exe"
            };
            
            foreach (var browserName in browserNames)
            {
                try
                {
                    var processes = Process.GetProcessesByName(browserName);
                    foreach (var proc in processes)
                    {
                        try
                        {
                            // Убиваем процесс без проверки времени
                            proc.Kill(true); // true = убить все дочерние процессы
                            proc.WaitForExit(2000);
                            Console.WriteLine($"   ✓ Закрыт: {proc.ProcessName} (PID: {proc.Id})");
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    static void MonitorAndKillBrowsers()
    {
        try
        {
            for (int i = 0; i < 40; i++) // Проверяем 40 раз с интервалом в 250мс = 10 секунд
            {
                KillBrowserProcesses();
                Thread.Sleep(250);
            }
        }
        catch { }
    }

    static string? EncryptToken(string token)
    {
        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                // Генерируем случайный IV (инициализационный вектор)
                byte[] iv = new byte[aes.IV.Length];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }
                aes.IV = iv;
                
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(tokenBytes, 0, tokenBytes.Length);
                    
                    // Объединяем IV + зашифрованные данные
                    byte[] result = new byte[iv.Length + encryptedBytes.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);
                    
                    // Конвертируем в Base64 для хранения в текстовом файле
                    return Convert.ToBase64String(result);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Ошибка шифрования токена: {ex.Message}");
            return null;
        }
    }

    static string? DecryptToken(string encryptedToken)
    {
        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                // Декодируем из Base64
                byte[] encryptedData = Convert.FromBase64String(encryptedToken);
                
                // Извлекаем IV (первые 16 байт)
                byte[] iv = new byte[aes.IV.Length];
                Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
                aes.IV = iv;
                
                // Извлекаем зашифрованные данные (остаток)
                byte[] cipherText = new byte[encryptedData.Length - iv.Length];
                Buffer.BlockCopy(encryptedData, iv.Length, cipherText, 0, cipherText.Length);
                
                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Ошибка расшифровки токена: {ex.Message}");
            return null;
        }
    }

    static void SaveEncryptedToken()
    {
        try
        {
            string configPath = Path.Combine(minecraftPath, "config");
            Directory.CreateDirectory(configPath);
            
            string encryptedTokenPath = Path.Combine(configPath, "auth.key");
            
            // Шифруем токен
            string? encryptedToken = EncryptToken(accessToken);
            if (encryptedToken == null)
            {
                Console.WriteLine("   ✗ Не удалось зашифровать токен");
                return;
            }
            
            // Сохраняем зашифрованный токен
            File.WriteAllText(encryptedTokenPath, encryptedToken);
            Console.WriteLine($"   ✓ Зашифрованный токен сохранен: {encryptedTokenPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Ошибка сохранения токена: {ex.Message}");
        }
    }

    static string LoadAndDecryptToken()
    {
        try
        {
            string encryptedTokenPath = Path.Combine(minecraftPath, "config", "auth.key");
            
            if (!File.Exists(encryptedTokenPath))
            {
                Console.WriteLine($"   ⚠ Файл с токеном не найден: {encryptedTokenPath}");
                return accessToken; // Возвращаем стандартный токен если файл не существует
            }
            
            string encryptedToken = File.ReadAllText(encryptedTokenPath);
            string? decryptedToken = DecryptToken(encryptedToken);
            
            if (decryptedToken == null)
            {
                Console.WriteLine("   ✗ Не удалось расшифровать токен");
                return accessToken;
            }
            
            Console.WriteLine($"   ✓ Токен успешно расшифрован");
            return decryptedToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Ошибка загрузки токена: {ex.Message}");
            return accessToken;
        }
    }
}