using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace FileAnalysisTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("  ___ _ _         _             _                       \r\n | __(_) |___    /_\\  _ _  __ _| |__ _ _  _ ___ ___ _ _ \r\n | _|| | / -_)  / _ \\| ' \\/ _ | / _ | || (_-</ -_) '_|\r\n |_| |_|_\\___| /_/ \\_\\_||_\\__,_|_\\__,_|\\_, /__/\\___|_|  \r\n                                       |__/             ");
            Console.WriteLine("\nType 'help' for a list of commands.");

            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                string[] commandParts = SplitCommand(input);
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
                        ExecuteStringExtraction(commandParts);
                        break;
                    case "comparebytes":
                        ExecuteCommandWithTwoVariables(commandParts, CompareBytes);
                        break;
                    case "comparetext":
                    case "comparelines":
                        ExecuteCommandWithTwoVariables(commandParts, CompareText);
                        break;
                    case "comparelinescontext":
                        ExecuteContextLineComparison(commandParts);
                        break;
                    case "comparebyteshexdump":
                        ExecuteCommandWithTwoVariables(commandParts, CompareBytesHexDump);
                        break;
                    case "searchstring":
                        ExecuteCommandWithTwoVariables(commandParts, StringSearch);
                        break;
                    case "searchstringnobreak":
                        ExecuteCommandWithTwoVariables(commandParts, StringSearches);
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
            Console.WriteLine("  comparetext <filePath1> <filePath2>   - Compare two text files character-by-character.");
            Console.WriteLine("  comparelines <filePath1> <filePath2>  - Compare two text files line-by-line.");
            Console.WriteLine("  comparelinescontext <filePath1> <filePath2> [contextLines] - Compare two text files with context.");
            Console.WriteLine("  comparebyteshexdump <filePath1> <filePath2> - Compare two files byte-by-byte in hex dump format.");
            Console.WriteLine("  searchstring <filePath> <searchString> - Search for a specific string in the specified file.");
            Console.WriteLine("  searchstringnobreak <filePath> <searchString> - Search for a specific string in the specified file without breaking.");
            Console.WriteLine("  exit                                   - Exit the application.");
        }


        public static string[] SplitCommand(string input)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string currentTerm = "";

            for (int i = 0; i < input.Length; i++)
            {
                char currentChar = input[i];

                // Check for quotes
                if (currentChar == '"')
                {
                    inQuotes = !inQuotes; // Toggle inQuotes
                    // If we are closing a quote, don't add the quote to the term
                    if (currentTerm.Length > 0 && !inQuotes)
                    {
                        result.Add(currentTerm);
                        currentTerm = ""; // Reset current term
                    }
                    continue;
                }

                // If we are not inside quotes and hit a space, we should finalize the term
                if (char.IsWhiteSpace(currentChar) && !inQuotes)
                {
                    if (currentTerm.Length > 0)
                    {
                        result.Add(currentTerm);
                        currentTerm = ""; // Reset current term
                    }
                }
                else
                {
                    // Append the current character to the current term
                    currentTerm += currentChar;
                }
            }

            // Add any remaining term at the end
            if (currentTerm.Length > 0)
            {
                result.Add(currentTerm);
            }

            return result.ToArray();
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

        static void ExecuteCommandWithTwoVariables(string[] commandParts, Action<string, string> command)
        {
            if (commandParts.Length < 3)
            {
                Console.WriteLine("Error: Two file paths must be provided.");
                return;
            }
            command(commandParts[1], commandParts[2]);
        }

        static void ExecuteStringExtraction(string[] commandParts)
        {
            if (commandParts.Length >= 2)
            {
                int minLength = commandParts.Length >= 3 ? int.Parse(commandParts[2]) : 4;
                ExtractStrings(commandParts[1], minLength);
            }
            else
            {
                Console.WriteLine("Error: No file path provided for string extraction.");
            }
        }

        static void ExecuteContextLineComparison(string[] commandParts)
        {
            if (commandParts.Length >= 3)
            {
                int contextLines = commandParts.Length >= 4 ? int.Parse(commandParts[3]) : 2;
                CompareLinesWithContext(commandParts[1], commandParts[2], contextLines);
            }
            else
            {
                Console.WriteLine("Error: Two file paths and context lines count are required.");
            }
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

        static void CompareBytes(string filePath1, string filePath2)
        {
            const string HighlightStart = "\x1b[31m"; // Red color start
            const string HighlightEnd = "\x1b[0m";    // Color end/reset
            const char DifferenceMarker = '■';        // Difference marker

            try
            {
                byte[] file1Bytes = System.IO.File.ReadAllBytes(filePath1);
                byte[] file2Bytes = System.IO.File.ReadAllBytes(filePath2);

                int maxLength = Math.Max(file1Bytes.Length, file2Bytes.Length);
                bool areFilesIdentical = true;

                for (int i = 0; i < maxLength; i++)
                {
                    byte byte1 = i < file1Bytes.Length ? file1Bytes[i] : (byte)0;
                    byte byte2 = i < file2Bytes.Length ? file2Bytes[i] : (byte)0;

                    if (byte1 == byte2)
                    {
                        Console.Write($"{byte1:X2} ");
                    }
                    else
                    {
                        Console.Write($"{HighlightStart}{DifferenceMarker}{HighlightEnd} ");
                        areFilesIdentical = false;
                    }
                }

                Console.WriteLine(); // Newline after the comparison output

                // Check if the files have different lengths
                if (file1Bytes.Length != file2Bytes.Length)
                {
                    Console.WriteLine($"{HighlightStart}Files have different lengths.{HighlightEnd}");
                    areFilesIdentical = false;
                }

                // Output the final result
                if (areFilesIdentical)
                {
                    Console.WriteLine("Files are identical.");
                }
                else
                {
                    Console.WriteLine("Comparison completed with differences found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void CompareText(string filePath1, string filePath2)
        {
            const string HighlightStart = "\x1b[31m"; // Red color start
            const string HighlightEnd = "\x1b[0m";    // Color end/reset
            const char DifferenceMarker = '■';        // Difference marker

            try
            {
                // Read the content of both files as strings
                string file1Text = System.IO.File.ReadAllText(filePath1);
                string file2Text = System.IO.File.ReadAllText(filePath2);

                int maxLength = Math.Max(file1Text.Length, file2Text.Length);
                bool areFilesIdentical = true;

                for (int i = 0; i < maxLength; i++)
                {
                    char char1 = i < file1Text.Length ? file1Text[i] : '\0';
                    char char2 = i < file2Text.Length ? file2Text[i] : '\0';

                    if (char1 == char2)
                    {
                        Console.Write(char1);
                    }
                    else
                    {
                        Console.Write($"{HighlightStart}{DifferenceMarker}{HighlightEnd}");
                        areFilesIdentical = false;
                    }
                }

                Console.WriteLine(); // Newline after the comparison output

                // Check if the files have different lengths
                if (file1Text.Length != file2Text.Length)
                {
                    Console.WriteLine($"{HighlightStart}Files have different lengths.{HighlightEnd}");
                    areFilesIdentical = false;
                }

                // Output the final result
                if (areFilesIdentical)
                {
                    Console.WriteLine("Files are identical.");
                }
                else
                {
                    Console.WriteLine("Comparison completed with differences found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void CompareLines(string filePath1, string filePath2)
        {
            const string HighlightStart = "\x1b[31m"; // Red color start
            const string HighlightEnd = "\x1b[0m";    // Color end/reset

            try
            {
                string[] file1Lines = System.IO.File.ReadAllLines(filePath1);
                string[] file2Lines = System.IO.File.ReadAllLines(filePath2);

                int maxLines = Math.Max(file1Lines.Length, file2Lines.Length);
                bool areFilesIdentical = true;

                for (int i = 0; i < maxLines; i++)
                {
                    string line1 = i < file1Lines.Length ? file1Lines[i] : string.Empty;
                    string line2 = i < file2Lines.Length ? file2Lines[i] : string.Empty;

                    if (line1 == line2)
                    {
                        Console.WriteLine(line1);
                    }
                    else
                    {
                        Console.WriteLine($"{HighlightStart}Difference at line {i}:{HighlightEnd}");
                        Console.WriteLine($"{HighlightStart}File1: {line1}{HighlightEnd}");
                        Console.WriteLine($"{HighlightStart}File2: {line2}{HighlightEnd}");
                        areFilesIdentical = false;
                    }
                }

                if (file1Lines.Length != file2Lines.Length)
                {
                    Console.WriteLine($"{HighlightStart}Files have different number of lines.{HighlightEnd}");
                    areFilesIdentical = false;
                }

                if (areFilesIdentical)
                {
                    Console.WriteLine("Files are identical.");
                }
                else
                {
                    Console.WriteLine("Comparison completed with differences found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void CompareLinesWithContext(string filePath1, string filePath2, int contextLines = 2)
        {
            const string HighlightStart = "\x1b[31m"; // Red color start
            const string HighlightEnd = "\x1b[0m";    // Color end/reset

            try
            {
                string[] file1Lines = System.IO.File.ReadAllLines(filePath1);
                string[] file2Lines = System.IO.File.ReadAllLines(filePath2);

                int maxLines = Math.Max(file1Lines.Length, file2Lines.Length);
                bool areFilesIdentical = true;

                for (int i = 0; i < maxLines; i++)
                {
                    string line1 = i < file1Lines.Length ? file1Lines[i] : string.Empty;
                    string line2 = i < file2Lines.Length ? file2Lines[i] : string.Empty;

                    if (line1 != line2)
                    {
                        // Show context before difference
                        int startContext = Math.Max(0, i - contextLines);
                        int endContext = Math.Min(maxLines, i + contextLines + 1);

                        for (int j = startContext; j < endContext; j++)
                        {
                            if (j == i)
                            {
                                Console.WriteLine($"{HighlightStart}Difference at line {j}:{HighlightEnd}");
                                Console.WriteLine($"{HighlightStart}File1: {file1Lines[j]}{HighlightEnd}");
                                Console.WriteLine($"{HighlightStart}File2: {file2Lines[j]}{HighlightEnd}");
                            }
                            else
                            {
                                Console.WriteLine($"File1: {file1Lines[j]}");
                                Console.WriteLine($"File2: {file2Lines[j]}");
                            }
                        }

                        areFilesIdentical = false;
                    }
                }

                if (file1Lines.Length != file2Lines.Length)
                {
                    Console.WriteLine($"{HighlightStart}Files have different number of lines.{HighlightEnd}");
                    areFilesIdentical = false;
                }

                if (areFilesIdentical)
                {
                    Console.WriteLine("Files are identical.");
                }
                else
                {
                    Console.WriteLine("Comparison completed with differences found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
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
                PrintHexDump(file1Data, file2Data, i);
                Console.WriteLine();
            }
        }

        static void PrintHexDump(byte[] data1, byte[] data2, int offset)
        {
            int length = Math.Min(16, data1.Length - offset);

            Console.Write($"File1: {offset:X4}  ");
            PrintHexLine(data1, offset, length, data2, true);

            Console.Write($"File2: {offset:X4}  ");
            PrintHexLine(data2, offset, length, data1, false);

            Console.WriteLine();
        }

        static void PrintHexLine(byte[] data, int offset, int length, byte[] otherData, bool isFile1)
        {
            for (int i = 0; i < length; i++)
            {
                if (data[offset + i] != otherData[offset + i])
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ResetColor();
                }

                Console.Write($"{data[offset + i]:X2} ");

                if (i == 15) Console.Write(new string(' ', (16 - length) * 3));
            }

            Console.ResetColor();
            Console.Write(" |");

            for (int i = 0; i < length; i++)
            {
                char c = (data[offset + i] >= 32 && data[offset + i] <= 126) ? (char)data[offset + i] : '.';
                if (data[offset + i] != otherData[offset + i])
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ResetColor();
                }

                Console.Write(c);
            }

            Console.ResetColor();
            Console.WriteLine("|");
        }

        static void StringSearch(string filepath, string targetString)
        {
            foreach (string line in File.ReadLines(filepath))
            {
                if (line.Contains(targetString))
                {
                    Console.WriteLine($"'{targetString}'found on line:");
                    Console.WriteLine(line);
                    return;
                }
            }
        }

        static void StringSearches(string filepath, string targetString)
        {
            int numInstances = 0;
            foreach (string line in File.ReadLines(filepath))
            {
                if (line.Contains(targetString))
                {
                    Console.WriteLine($"'{targetString}'found on line:");
                    Console.WriteLine(line);
                    numInstances++;
                }
            }

            Console.WriteLine($"Word was found {numInstances} times.");
            return;
        }

    }
}
