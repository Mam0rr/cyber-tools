using System;
using System.Collections.Generic;
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
                        ExecuteCommandWithFilePath(commandParts, CalculateEntropy);
                        break;

                    case "metadata":
                        ExecuteCommandWithFilePath(commandParts, ExtractMetadata);
                        break;

                    case "strings":
                        if (commandParts.Length >= 2)
                        {
                            int minLength = commandParts.Length >= 3 ? int.Parse(commandParts[2]) : 4;
                            ExtractStrings(commandParts[1], minLength);
                        }
                        else
                        {
                            Console.WriteLine("Error: No file path provided for string extraction.");
                        }
                        break;

                    case "comparebytes":
                        ExecuteCommandWithTwoFilePaths(commandParts, CompareBytes);
                        break;

                    case "comparetext":
                        ExecuteCommandWithTwoFilePaths(commandParts, CompareText);
                        break;

                    case "comparelines":
                        ExecuteCommandWithTwoFilePaths(commandParts, CompareLines);
                        break;

                    case "comparelinescontext":
                        if (commandParts.Length >= 3)
                        {
                            int contextLines = commandParts.Length >= 4 ? int.Parse(commandParts[3]) : 2;
                            CompareLinesWithContext(commandParts[1], commandParts[2], contextLines);
                        }
                        else
                        {
                            Console.WriteLine("Error: Two file paths and context lines count are required.");
                        }
                        break;

                    case "comparebyteshexdump":
                        ExecuteCommandWithTwoFilePaths(commandParts, CompareBytesHexDump);
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
            Console.WriteLine("  entropy <filePath>                     - Calculate the entropy of the specified file.");
            Console.WriteLine("  metadata <filePath>                    - Extract and display metadata from the specified file.");
            Console.WriteLine("  strings <filePath> [minLength]         - Extract and display printable strings from the specified file.");
            Console.WriteLine("  comparebytes <filePath1> <filePath2>  - Compare two files byte-by-byte.");
            Console.WriteLine("  comparetext <filePath1> <filePath2>   - Compare two text files line-by-line.");
            Console.WriteLine("  comparelines <filePath1> <filePath2>  - Compare two text files line-by-line.");
            Console.WriteLine("  comparelinescontext <filePath1> <filePath2> [contextLines] - Compare two text files with context.");
            Console.WriteLine("  comparebyteshexdump <filePath1> <filePath2> - Compare two files byte-by-byte in hex dump format.");
            Console.WriteLine("  exit                                   - Exit the application.");
        }

        static void ExecuteCommandWithFilePath(string[] commandParts, Action<string> command)
        {
            if (commandParts.Length < 2)
            {
                Console.WriteLine("Error: No file path provided.");
                return;
            }
            command(commandParts[1]);
        }

        static void ExecuteCommandWithTwoFilePaths(string[] commandParts, Action<string, string> command)
        {
            if (commandParts.Length < 3)
            {
                Console.WriteLine("Error: Two file paths must be provided.");
                return;
            }
            command(commandParts[1], commandParts[2]);
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

            var byteFreq = data.GroupBy(b => b).ToDictionary(g => g.Key, g => g.Count());
            var probabilities = byteFreq.Values.Select(frequency => (double)frequency / totalBytes);

            double entropy = probabilities.Where(p => p > 0).Sum(p => -p * Math.Log2(p));

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

            string fileExtension = Path.GetExtension(filePath).ToLower();
            switch (fileExtension)
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".gif":
                case ".tiff":
                    Console.WriteLine("\nImage Metadata:");
                    PrintImageMetadata(filePath);
                    break;
                case ".txt":
                    Console.WriteLine("\nText File Metadata:");
                    PrintTextFileMetadata(filePath);
                    break;
                case ".pdf":
                    Console.WriteLine("\nPDF Metadata:");
                    PrintPdfMetadata(filePath);
                    break;
                default:
                    Console.WriteLine("Error: Unsupported file type for extended metadata extraction.");
                    break;
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
                        string value = prop.Type switch
                        {
                            1 => BitConverter.ToString(prop.Value), // BYTE
                            2 => Encoding.ASCII.GetString(prop.Value), // ASCII
                            3 => BitConverter.ToUInt16(prop.Value, 0).ToString(), // SHORT
                            4 => BitConverter.ToUInt32(prop.Value, 0).ToString(), // LONG
                            7 => BitConverter.ToString(prop.Value), // UNDEFINED
                            9 => BitConverter.ToInt32(prop.Value, 0).ToString(), // SLONG
                            _ => BitConverter.ToString(prop.Value) // Default
                        };

                        Console.WriteLine($"\nProperty ID: {prop.Id}");
                        Console.WriteLine("Type: " + prop.Type);
                        Console.WriteLine("Length: " + prop.Len);
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
                using (PdfDocument pdf = PdfDocument.Open(filePath))
                {
                    Console.WriteLine("PDF Metadata:");
                    Console.WriteLine("Number of Pages: " + pdf.NumberOfPages);

                    var info = pdf.Information;
                    Console.WriteLine("Title: " + info.Title);
                    Console.WriteLine("Author: " + info.Author);
                    Console.WriteLine("Subject: " + info.Subject);
                    Console.WriteLine("Keywords: " + info.Keywords);
                    Console.WriteLine("Creator: " + info.Creator);
                    Console.WriteLine("Producer: " + info.Producer);
                    Console.WriteLine("Creation Date: " + info.CreationDate);
                    Console.WriteLine("Modification Date: " + info.ModifiedDate);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Unable to read PDF metadata. {ex.Message}");
            }
        }

        static Encoding GetFileEncoding(string filePath)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            if (fileBytes.Length >= 3)
            {
                if (fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
                    return Encoding.UTF8; // BOM for UTF-8
                if (fileBytes[0] == 0xFF && fileBytes[1] == 0xFE)
                    return Encoding.Unicode; // BOM for UTF-16LE
                if (fileBytes[0] == 0xFE && fileBytes[1] == 0xFF)
                    return Encoding.BigEndianUnicode; // BOM for UTF-16BE
            }
            return Encoding.Default;
        }

        static void ExtractStrings(string filePath, int minLength)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found: {filePath}");
                return;
            }

            byte[] data = File.ReadAllBytes(filePath);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] >= 32 && data[i] <= 126)
                {
                    sb.Append((char)data[i]);
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

            if (sb.Length >= minLength)
            {
                Console.WriteLine(sb.ToString());
            }
        }

        static void CompareBytes(string filePath1, string filePath2)
        {
            if (!File.Exists(filePath1) || !File.Exists(filePath2))
            {
                Console.WriteLine("Error: One or both files not found.");
                return;
            }

            byte[] file1Data = File.ReadAllBytes(filePath1);
            byte[] file2Data = File.ReadAllBytes(filePath2);

            if (file1Data.Length != file2Data.Length)
            {
                Console.WriteLine("Files have different sizes.");
                return;
            }

            for (int i = 0; i < file1Data.Length; i++)
            {
                if (file1Data[i] != file2Data[i])
                {
                    Console.WriteLine($"Files differ at byte position {i}.");
                    return;
                }
            }

            Console.WriteLine("Files are identical.");
        }

        static void CompareText(string filePath1, string filePath2)
        {
            if (!File.Exists(filePath1) || !File.Exists(filePath2))
            {
                Console.WriteLine("Error: One or both files not found.");
                return;
            }

            string[] file1Lines = File.ReadAllLines(filePath1);
            string[] file2Lines = File.ReadAllLines(filePath2);

            int maxLines = Math.Max(file1Lines.Length, file2Lines.Length);
            bool areIdentical = true;

            for (int i = 0; i < maxLines; i++)
            {
                string line1 = i < file1Lines.Length ? file1Lines[i] : null;
                string line2 = i < file2Lines.Length ? file2Lines[i] : null;

                if (line1 != line2)
                {
                    Console.WriteLine($"Files differ at line {i + 1}:");
                    Console.WriteLine($"File1: {(line1 ?? "<no line>")}");
                    Console.WriteLine($"File2: {(line2 ?? "<no line>")}");
                    areIdentical = false;
                }
            }

            if (areIdentical)
            {
                Console.WriteLine("Files are identical.");
            }
        }

        static void CompareLines(string filePath1, string filePath2)
        {
            CompareText(filePath1, filePath2); // Reuse CompareText method
        }

        static void CompareLinesWithContext(string filePath1, string filePath2, int contextLines)
        {
            if (!File.Exists(filePath1) || !File.Exists(filePath2))
            {
                Console.WriteLine("Error: One or both files not found.");
                return;
            }

            string[] file1Lines = File.ReadAllLines(filePath1);
            string[] file2Lines = File.ReadAllLines(filePath2);

            int maxLines = Math.Max(file1Lines.Length, file2Lines.Length);
            bool areIdentical = true;

            for (int i = 0; i < maxLines; i++)
            {
                string line1 = i < file1Lines.Length ? file1Lines[i] : null;
                string line2 = i < file2Lines.Length ? file2Lines[i] : null;

                if (line1 != line2)
                {
                    Console.WriteLine($"Files differ at line {i + 1}:");

                    PrintContext(file1Lines, i, contextLines, "File1");
                    PrintContext(file2Lines, i, contextLines, "File2");

                    areIdentical = false;
                }
            }

            if (areIdentical)
            {
                Console.WriteLine("Files are identical.");
            }
        }

        static void PrintContext(string[] lines, int currentIndex, int contextLines, string fileLabel)
        {
            int start = Math.Max(0, currentIndex - contextLines);
            int end = Math.Min(lines.Length, currentIndex + contextLines + 1);

            Console.WriteLine($"{fileLabel} Context:");
            for (int i = start; i < end; i++)
            {
                Console.WriteLine($"{(i == currentIndex ? ">> " : "   ")}{lines[i]}");
            }
        }

        static void CompareBytesHexDump(string filePath1, string filePath2)
        {
            if (!File.Exists(filePath1) || !File.Exists(filePath2))
            {
                Console.WriteLine("Error: One or both files not found.");
                return;
            }

            byte[] file1Data = File.ReadAllBytes(filePath1);
            byte[] file2Data = File.ReadAllBytes(filePath2);

            if (file1Data.Length != file2Data.Length)
            {
                Console.WriteLine("Files have different sizes.");
                return;
            }

            for (int i = 0; i < file1Data.Length; i += 16)
            {
                Console.WriteLine($"Offset {i:X4}:");
                PrintHexDump(file1Data, i);
                PrintHexDump(file2Data, i);
                Console.WriteLine();
            }
        }

        static void PrintHexDump(byte[] data, int offset)
        {
            int length = Math.Min(16, data.Length - offset);

            Console.Write($"File Data: {offset:X4}  ");

            for (int i = 0; i < length; i++)
            {
                Console.Write($"{data[offset + i]:X2} ");
            }

            Console.Write(new string(' ', (16 - length) * 3));

            Console.Write(" |");

            for (int i = 0; i < length; i++)
            {
                char c = (data[offset + i] >= 32 && data[offset + i] <= 126) ? (char)data[offset + i] : '.';
                Console.Write(c);
            }

            Console.WriteLine("|");
        }
    }
}
