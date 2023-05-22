using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace SPI_ISBNValidator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IsbnController : Controller
    {
        [HttpPost]
        public IActionResult ValidateISBN([FromBody] string isbn)
        {
            string input = isbn ?? "";

            ISBNChecker checker = new ISBNChecker(input);

            // Return result as JSON
            return Ok(new { validformat = checker.isFormatValid, validisbn = checker.isValid, type = checker.typeISBN });          
        }

        [HttpGet]
        public IActionResult test()
        {
            // Return result as JSON
            return Ok(new {result = "welcome"});
            //return View();
        }
    }

    public class ISBNChecker
    {

        private readonly Regex isbn10FormatRegex;
        private readonly Regex isbn13FormatRegex;

        public bool isFormatValid = true;
        public bool isValid = false;
        public string typeISBN = "";

        public ISBNChecker(string input)
        {
            //Use regular expressions to check the correctness of the format,
            //e.g. for ISBN-10: 7-309-04547-5 or 7-309-04547-X or 7 309 04547 5 or 7309045475 
            isbn10FormatRegex = new Regex(@"^\d+[- ]\d+[- ]\d+[- ][0-9xX]$|^[0-9]{9}[0-9xX]$");
            //e.g. for ISBN-13: 978-986-181-728-6 or 978 986 181 728 6 or 9789861817286
            isbn13FormatRegex = new Regex(@"^\d+[- ]\d+[- ]\d+[- ]\d+[- ][0-9]$|^[0-9]{13}$");

            isValid = CheckISBN(input);
        }

        public bool CheckISBN(string input)
        {
            string cleanedInput = CleanInput(input);

            if (CheckFormatISBN10(cleanedInput))
            {
                this.typeISBN = "ISBN-10";
                return CheckValidityISBN10(cleanedInput);
            }
            else if (CheckFormatISBN13(cleanedInput))
            {
                this.typeISBN = "ISBN-13";
                return CheckValidityISBN13(cleanedInput);
            }
            else
            {
                this.isFormatValid = false;
                return false;
            }
        }

        private string CleanInput(string input)
        {
            //Replace multiple consecutive spaces in a string with a single space
            string cleanedInput = Regex.Replace(input, @"\s+", " ").Trim();
            return cleanedInput;
        }

        public bool CheckFormatISBN10(string input)
        {
            isFormatValid = isbn10FormatRegex.IsMatch(input) && Regex.Replace(input, @"[\s-]", "").Length == 10;

            return isFormatValid;
        }

        public bool CheckFormatISBN13(string input)
        {
            isFormatValid = isbn13FormatRegex.IsMatch(input) && Regex.Replace(input, @"[\s-]", "").Length == 13;

            return isFormatValid;
        }

        private bool CheckValidityISBN10(string input)
        {
            input = Regex.Replace(input, @"[\s-]", "");
            int checksum = 0;
            for (int i = 0; i < 9; i++)
            {
                checksum += int.Parse(input[i].ToString()) * (10 - i);
            }
            //check if the last character is number.
            //If not number (it's X) then convert it to 10 
            int lastDigit = !Char.IsDigit(input[9]) ? 10 : int.Parse(input[9].ToString());
            checksum += lastDigit;

            return checksum % 11 == 0;
        }

        private bool CheckValidityISBN13(string input)
        {
            input = Regex.Replace(input, @"[\s-]", "");
            int checksum = 0;
            for (int i = 0; i < 12; i++)
            {
                checksum += (i % 2 == 0) ? int.Parse(input[i].ToString()) : int.Parse(input[i].ToString()) * 3;
            }
            int lastDigit = int.Parse(input[12].ToString());
            checksum += lastDigit;

            return checksum % 10 == 0;
        }
    }
}
