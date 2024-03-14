using AudioTOBase64.Models;
using AudioTOBase64.Repository;
using Microsoft.AspNetCore.Mvc;
using NAudio.Wave;
using System.Runtime.InteropServices;

namespace AudioTOBase64.Controllers
{
    public class AudioController : Controller
    {
        private MemoryStream memoryStream;
        private WaveInEvent waveIn;
        private static string encodedAudio;

        private readonly int _recordingDuration = 10;
        //Voice Liveness
        private readonly IConfiguration _configuration;
        private readonly ILogger<AudioController> _logger;
        public AudioController(IConfiguration configuration, ILogger<AudioController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
       
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Record(int i)
        {
            return View();
        }

        [HttpPost]
        public IActionResult StartRecording()
        {
            memoryStream = new MemoryStream();
            waveIn = new WaveInEvent();

            // Configure WaveInEvent
            waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz sampling rate, mono (1 channel)
            waveIn.DataAvailable += (sender, e) =>
            {
                // Write recorded audio to the MemoryStream
                memoryStream.Write(e.Buffer, 0, e.BytesRecorded);
            };

            // Start recording
            waveIn.StartRecording();

            // Set up a timer to stop recording after 10 seconds
            Task.Delay(10000).ContinueWith(_ => StopRecording());

            return Ok(); // Indicate successful start of recording
        }

        [HttpPost]
        public IActionResult StopRecording()
        {
            if (waveIn != null)
            {
                // Stop recording
                waveIn.StopRecording();
                waveIn.Dispose();
                // Write the WAV header to the beginning of the MemoryStream
                WriteWavHeader(memoryStream);

                // Perform further processing or storage as needed
                encodedAudio = ConvertToBase64(memoryStream.ToArray());
                ViewBag.EncodedAudio = encodedAudio;

            }

            return Ok(); // Indicate successful stop of recording
        }



        private void WriteWavHeader(MemoryStream stream)
        {
            if (stream != null)
            {
                // Get the audio data length
                long audioDataLength = stream.Length - 44; // Subtracting the length of the header (44 bytes)

                // Write the WAV header to the beginning of the MemoryStream
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Seek(0, SeekOrigin.Begin);
                writer.Write(0x46464952); // "RIFF" in ASCII
                writer.Write((int)(audioDataLength + 36)); // File size - 8
                writer.Write(0x45564157); // "WAVE" in ASCII
                writer.Write(0x20746D66); // "fmt " in ASCII
                writer.Write((int)16); // Subchunk1Size: PCM format (16)
                writer.Write((short)1); // AudioFormat: PCM
                writer.Write((short)1); // NumChannels: Mono (1 channel)
                writer.Write((int)16000); // SampleRate: 16kHz
                writer.Write((int)(16000 * 1 * 16 / 8)); // ByteRate: SampleRate * NumChannels * BitsPerSample / 8
                writer.Write((short)(1 * 16 / 8)); // BlockAlign: NumChannels * BitsPerSample / 8
                writer.Write((short)16); // BitsPerSample: 16
                writer.Write(0x61746164); // "data" in ASCII
                writer.Write((int)audioDataLength); // Subchunk2Size: audio data length
                writer.Seek(0, SeekOrigin.End); // Seek to the end of the MemoryStream
            }
        }

        private string ConvertToBase64(byte[] audioData)
        {
            // Convert to Base64
            return Convert.ToBase64String(audioData);
        }


        public class EmulatorDetector
        {
            // Import the Windows API function to check if the application is running in an emulator
            // Import the Windows API function to check if the application is running in an emulator
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern IntPtr GetModuleHandle(string lpModuleName);


            public static bool IsEmulator()
            {
                // Check if any emulator-related module is loaded in memory
                // Example: Android emulator module name
                IntPtr moduleHandle = GetModuleHandle("qemu-system-i386");
                IntPtr moduleHandle64 = GetModuleHandle("qemu-system-x86_64");

                return moduleHandle != IntPtr.Zero || moduleHandle64 != IntPtr.Zero;
            }

        }

    public IActionResult audioRecording()
        {
            
            if (IsRunningOnEmulator())
            {
                return View("emulator_Found");
            }
            else if (RootChecker.IsDeviceRooted())
            {
                return View("emulator_Found");
            }
            else
            {
                Audio audioRepository = new Audio(_configuration);
                string promptValue;
                promptValue = audioRepository.GetRandomPromptsFromDatabase();
                ViewBag.PromptValue = promptValue;
                Class model = new Class();
                TempData["promptValue"] = promptValue;
                return View();
                // return View("emulator_Found");
            }
        }
        public static bool IsRunningOnEmulator()
        {
            // Detecting Android Emulator
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Check if running on the Android Emulator
                return DetectAndroidEmulator();
            }

            // Add detection logic for other platforms (if applicable)

            // Default to assuming not running on an emulator
            return false;
        }

        private static bool DetectAndroidEmulator()
        {
            // Check if running on the Android Emulator by examining system properties
            string product = Environment.GetEnvironmentVariable("ANDROID_PRODUCT");
            string manufacturer = Environment.GetEnvironmentVariable("ANDROID_MANUFACTURER");

            // Possible emulator product and manufacturer values
            string[] emulatorProducts = { "sdk", "google_sdk", "generic", "VirtualBox", "VMware Workstation", "QEMU", "DOSBox", "PCSX2", "Dolphin", "Citra", "RPCS3", "MAME", "BlueStacks", "Xamarin Android Player", "Apple iOS Simulator", "Genymotion", "Andyroid", "Nox App Player" };
            string[] emulatorManufacturers = { "unknown", "Genymotion", "Virtual", "Oracle Corporation", "VMware, Inc.", "QEMU Project", "DOSBox Team", "PCSX2 Team", "Dolphin Emulator Project", "Citra Team", "RPCS3 Team", "MAME Development Team", "BlueStacks Inc.", "Xamarin (Microsoft)", "Apple Inc.", "Andyroid", "Nox Digital Entertainment Co. Limited" };

            // Check if product or manufacturer matches any of the possible emulator values
            if ((!string.IsNullOrEmpty(product) && emulatorProducts.Contains(product.ToLower())) ||
                (!string.IsNullOrEmpty(manufacturer) && emulatorManufacturers.Contains(manufacturer.ToLower())))
            {
                return true; // Running on emulator
            }

            return false; // Not running on emulator
        }


        [HttpPost]
        public async Task<IActionResult> Record()
        {
            if (Request.HasFormContentType)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await Request.Body.CopyToAsync(memoryStream);
                    var audioData = memoryStream.ToArray();

                    // Simulate saving in memory (replace with actual storage logic)
                    var recording = new Class
                    {
                        AudioData = audioData,
                        FileName = "recording.wav"
                    };

                    return Json(recording);
                }
            }
            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> audioRecording(Class model)
        {
            if (encodedAudio != null)
            {
                model.File = encodedAudio;
            }
            if (model.File == null || model.File == null || model.File.Length == 0)
            {
                return BadRequest("Invalid file");
            }

            try
            {
                Audio audioRepository = new Audio(_configuration);
                model.Prompts = (string)TempData["promptValue"];
                string response = await audioRepository.PostAudio(model);              
                TempData["result"] = response;
                ViewBag.result = response;
                
                //_logger.LogInformation(model.Email,model.EmployeeID);
                return View();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }


       

    }
}