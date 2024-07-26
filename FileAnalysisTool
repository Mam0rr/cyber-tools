using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;

namespace FileAnalysisTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("  ___ _ _         _             _                       \r\n | __(_) |___    /_\\  _ _  __ _| |__ _ _  _ ___ ___ _ _ \r\n | _|| | / -_)  / _ \\| ' \\/ _` | / _` | || (_-</ -_) '_|\r\n |_| |_|_\\___| /_/ \\_\\_||_\\__,_|_\\__,_|\\_, /__/\\___|_|  \r\n                                       |__/             ");
            Console.WriteLine("\nType 'help' for a list of commands.");

            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                string[] commandParts = input.Split(' ');
                string command = commandParts[0].ToLower();

                switch (command)
                {
                    case "help":
                        ShowHelp();
                        break;

                    case "entropy":
                        if (commandParts.Length < 2)
                        {
                            Console.WriteLine("Error: No file path provided for entropy calculation.");
                            break;
                        }
                        CalculateEntropy(commandParts[1]);
                        break;

                    case "metadata":
                        if (commandParts.Length < 2)
                        {
                            Console.WriteLine("Error: No file path provided for metadata extraction.");
                            break;
                        }
                        ExtractMetadata(commandParts[1]);
                        break;

                    case "strings":
                        if (commandParts.Length == 2 || int.Parse(commandParts[2]) <= 0)
                        {
                            ExtractStrings(commandParts[1], 4);
                        }
                        else
                        {
                            ExtractStrings(commandParts[1], int.Parse(commandParts[2]));
                        }
                        break;


                    case "exit":
                        Console.WriteLine("Exiting...");
                        return;

                    default:
                        Console.WriteLine("Error: Unknown command. Type 'help' for a list of available commands.");
                        break;
                }
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("\nAvailable commands:");
            Console.WriteLine("  entropy <filePath>      - Calculate the entropy of the specified file.");
            Console.WriteLine("  metadata <filePath>     - Extract and display metadata from the specified file.");
            Console.WriteLine("  strings <filePath> [minLength] - Extract and display printable strings from the specified file.");
            Console.WriteLine("  exit                    - Exit the application.");
        }

        static void CalculateEntropy(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found: {filePath}");
                return;
            }

            byte[] data = File.ReadAllBytes(filePath);
            int totalBytes = data.Length;

            if (totalBytes == 0)
            {
                Console.WriteLine("Error: File is empty.");
                return;
            }

            Dictionary<byte, int> byteFreq = new Dictionary<byte, int>();
            foreach (byte b in data)
            {
                if (byteFreq.ContainsKey(b))
                {
                    byteFreq[b]++;
                }
                else
                {
                    byteFreq[b] = 1;
                }
            }

            var probabilities = byteFreq.Values.Select(frequency => (double)frequency / totalBytes);

            double entropy = 0;
            foreach (double p in probabilities)
            {
                if (p > 0)
                {
                    entropy -= p * Math.Log2(p);
                }
            }

            Console.WriteLine($"Entropy of the file: {entropy:F4} bits per byte");
        }

        static void ExtractMetadata(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found: {filePath}");
                return;
            }

            FileInfo fileInfo = new FileInfo(filePath);

            Console.WriteLine("File Name: " + fileInfo.Name);
            Console.WriteLine("File Size: " + fileInfo.Length + " bytes");
            Console.WriteLine("Creation Time: " + fileInfo.CreationTime);
            Console.WriteLine("Last Access Time: " + fileInfo.LastAccessTime.ToLongDateString());
            Console.WriteLine("Last Write Time: " + fileInfo.LastWriteTime.ToShortDateString());

            // Check if the file has an image extension
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff" };
            string fileExtension = Path.GetExtension(filePath).ToLower();

            if (Array.Exists(imageExtensions, ext => ext.Equals(fileExtension)))
            {
                Console.WriteLine("\nImage Metadata:");
                PrintImageMetadata(filePath);
            }
            else if (fileExtension == ".txt")
            {
                Console.WriteLine("\nText File Metadata:");
                PrintTextFileMetadata(filePath);
            }
            else if (fileExtension == ".pdf")
            {
                Console.WriteLine("\nPDF Metadata:");
                PrintPdfMetadata(filePath);
            }
            else
            {
                Console.WriteLine("Error: Unsupported file type for extended metadata extraction.");
            }
        }

        static void PrintImageMetadata(string filePath)
        {
            try
            {
                using (Image image = Image.FromFile(filePath))
                {
                    Console.WriteLine("Width: " + image.Width);
                    Console.WriteLine("Height: " + image.Height);
                    Console.WriteLine("Pixel Format: " + image.PixelFormat);

                    foreach (PropertyItem prop in image.PropertyItems)
                    {
                        Console.WriteLine($"\nProperty ID: {prop.Id}");
                        Console.WriteLine("Type: " + prop.Type);
                        Console.WriteLine("Length: " + prop.Len);

                        // Convert the property value based on type
                        string value = prop.Type switch
                        {
                            1 => BitConverter.ToString(prop.Value), // BYTE
                            2 => System.Text.Encoding.ASCII.GetString(prop.Value), // ASCII
                            3 => BitConverter.ToUInt16(prop.Value, 0).ToString(), // SHORT
                            4 => BitConverter.ToUInt32(prop.Value, 0).ToString(), // LONG
                            7 => BitConverter.ToString(prop.Value), // UNDEFINED
                            9 => BitConverter.ToInt32(prop.Value, 0).ToString(), // SLONG
                            _ => BitConverter.ToString(prop.Value) // Default
                        };

                        Console.WriteLine("Value: " + value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Unable to read image metadata. {ex.Message}");
            }
        }

        static void PrintTextFileMetadata(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);

                Encoding encoding = GetFileEncoding(filePath);

                int lineCount = content.Split('\n').Length;
                int wordCount = content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                int charCount = content.Length;

                Console.WriteLine("File Encoding: " + encoding.EncodingName);
                Console.WriteLine("Line Count: " + lineCount);
                Console.WriteLine("Word Count: " + wordCount);
                Console.WriteLine("Character Count: " + charCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Unable to read text file metadata. {ex.Message}");
            }
        }

        static void PrintPdfMetadata(string filePath)
        {
            try
            {
                using (PdfDocument pdfDoc = PdfDocument.Open(filePath))
                {
                    var documentInfo = pdfDoc.Information;

                    Console.WriteLine("Title: " + (string.IsNullOrEmpty(documentInfo.Title) ? "Not available" : documentInfo.Title));
                    Console.WriteLine("Author: " + (string.IsNullOrEmpty(documentInfo.Author) ? "Not available" : documentInfo.Author));
                    Console.WriteLine("Subject: " + (string.IsNullOrEmpty(documentInfo.Subject) ? "Not available" : documentInfo.Subject));
                    Console.WriteLine("Keywords: " + (string.IsNullOrEmpty(documentInfo.Keywords) ? "Not available" : documentInfo.Keywords));
                    Console.WriteLine("Creator: " + (string.IsNullOrEmpty(documentInfo.Creator) ? "Not available" : documentInfo.Creator));
                    Console.WriteLine("Producer: " + (string.IsNullOrEmpty(documentInfo.Producer) ? "Not available" : documentInfo.Producer));
                    Console.WriteLine("Creation Date: " + FormatPdfDate(documentInfo.CreationDate));
                    Console.WriteLine("Modification Date: " + FormatPdfDate(documentInfo.ModifiedDate));
                    Console.WriteLine("Page Count: " + pdfDoc.NumberOfPages);
                    Console.WriteLine("Encrypted: " + (pdfDoc.IsEncrypted ? "True" : "False"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Unable to read PDF metadata. {ex.Message}");
            }
        }

        static string FormatPdfDate(string pdfDate)
        {
            if (string.IsNullOrEmpty(pdfDate) || !pdfDate.StartsWith("D:"))
            {
                return "Not available";
            }

            try
            {
                var dateString = pdfDate.Substring(2);
                var year = dateString.Substring(0, 4);
                var month = dateString.Substring(4, 2);
                var day = dateString.Substring(6, 2);
                var hour = dateString.Substring(8, 2);
                var minute = dateString.Substring(10, 2);
                var second = dateString.Substring(12, 2);
                var timeZone = dateString.Substring(14);

                return $"{year}-{month}-{day} {hour}:{minute}:{second} {timeZone}";
            }
            catch
            {
                return "Invalid date format";
            }
        }

        static Encoding GetFileEncoding(string filePath)
        {
            using (var reader = new StreamReader(filePath, Encoding.Default, detectEncodingFromByteOrderMarks: true))
            {
                return reader.CurrentEncoding;
            }
        }

        static void ExtractStrings(string filePath, int minLength)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found: {filePath}");
                return;
            }

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream))
                {
                    StringBuilder sb = new StringBuilder();
                    byte[] buffer = reader.ReadBytes((int)reader.BaseStream.Length);
                    foreach (byte b in buffer)
                    {
                        if (IsPrintable(b))
                        {
                            sb.Append((char)b);
                        }
                        else
                        {
                            if (sb.Length >= minLength)
                            {
                                Console.WriteLine(sb.ToString());
                            }
                            sb.Clear();
                        }
                    }

                    // Print the last string if the file ends with printable characters
                    if (sb.Length >= minLength)
                    {
                        Console.WriteLine(sb.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Unable to extract strings. {ex.Message}");
            }
        }

        // Determine if a byte is a printable ASCII character
        static bool IsPrintable(byte b)
        {
            return b >= 32 && b <= 126;
        }
    }
}
