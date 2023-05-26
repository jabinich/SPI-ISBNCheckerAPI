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

            ISBNChecker checker = ISBNChecker.GetInstance();
            checker.CheckISBN(input);

            // Return result as JSON
            return Ok(new { isformat = checker.IsFormatValid, 
                            isvalid = checker.IsISBNValid, 
                            type = checker.ISBNFormat,
                            isbn = checker.FinalISBN });          
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
        private static ISBNChecker? instance;

        private readonly Regex isbn10FormatRegex;
        private readonly Regex isbn13FormatRegex;

        private readonly Regex isbn9FormatRegex; //for isbn-10 without check-digit
        private readonly Regex isbn12FormatRegex; //for isbn-13 without check-digit

        public bool IsFormatValid { get; private set; }
        public bool IsISBNValid { get; private set; }
        public string ISBNFormat { get; private set; }
        public string FinalISBN { get; private set; }

        public ISBNChecker()
        {
            //Use regular expressions to check the correctness of the format,
            //for ISBN-10:
            //e.g. 7-309-04547-5,7-309-04547-X using \d+\-\d+\-\d+\-[0-9xX]
            //or 7 309 04547 5 using \d+\ \d+\ \d+\ [0-9xX]
            //or 7309045475 using [0-9]{9}[0-9xX] 
            //And extend the expression above with 'positive Lookahead' to restrict the length:
            //using (?=(?:\D*\d){9}\D*[0-9xX]$)

            isbn10FormatRegex = new Regex(@"^(?=(?:\D*\d){9}\D*[0-9xX]$)(\d+\-\d+\-\d+\-[0-9xX]$|\d+\ \d+\ \d+\ [0-9xX]$|[0-9]{9}[0-9xX])$");

            //for ISBN-13:
            //e.g. 978-986-181-728-6 using \d{3}\-\d+\-\d+\-\d+\-[0-9]
            //or 978 986 181 728 6 using \d{3}\ \d+\ \d+\ \d+\ [0-9]
            //or 9789861817286 using [0-9]{13}
            //And extend the expression above with 'positive Lookahead' to restrict the length:
            //using (?=(?:\D*\d){13}$)

            isbn13FormatRegex = new Regex(@"^(?=(?:\D*\d){13}\D*$)(\d{3}\-\d+\-\d+\-\d+\-[0-9]$|\d{3}\ \d+\ \d+\ \d+\ [0-9]$|[0-9]{13})$");

            //for ISBN-10 without check-digit:
            //e.g. 7-309-04547-,7-309-04547 using \d+\-\d+\-\d+\-?
            //or 7 309 04547 using \d+\ \d+\ \d+
            //or 7309045475 using [0-9]{9}
            //And extend the expression above with 'positive Lookahead' to restrict the length:
            //using (?=(?:\D*\d){9}\D*$)

            isbn9FormatRegex = new Regex(@"^(?=(?:\D*\d){9}\D*$)(\d+\-\d+\-\d+\-?$|\d+\ \d+\ \d+$|[0-9]{9})$");

            //for ISBN-13 withou check-digit:
            //e.g. 978-986-181-728-,978-986-181-728 using \d{3}\-\d+\-\d+\-\d+\-?
            //or 978 986 181 728 using \d{3}\ \d+\ \d+\ \d+
            //or 978986181728 using [0-9]{12}
            //And extend the expression above with 'positive Lookahead' to restrict the length:
            //using (?=(?:\D*\d){12}\D*$)

            isbn12FormatRegex = new Regex(@"^(?=(?:\D*\d){12}\D*$)(\d{3}\-\d+\-\d+\-\d+\-?$|\d{3}\ \d+\ \d+\ \d+$|[0-9]{12})$");


            ISBNFormat = string.Empty;
            FinalISBN = string.Empty;
        }

        // Public static method to access the singleton instance
        public static ISBNChecker GetInstance()
        {
            instance ??= new ISBNChecker();

            return instance;
        }

        public void CheckISBN(string input)
        {
            string cleanedInput = CleanInput(input);

            if (CheckFormatISBN10(cleanedInput))
            {
                IsFormatValid = true;
                IsISBNValid = CheckValidityISBN10(cleanedInput);
                ISBNFormat = "ISBN-10";
                //if ISBN is not valid, correct it
                FinalISBN = IsISBNValid ? cleanedInput : GetFinalISBN(GetCheckdigitISBN10, cleanedInput);
            }
            else if (CheckFormatISBN13(cleanedInput))
            {
                IsFormatValid = true;
                IsISBNValid = CheckValidityISBN13(cleanedInput);
                ISBNFormat = "ISBN-13";
                //if ISBN is not valid, correct it
                FinalISBN = IsISBNValid ? cleanedInput : GetFinalISBN(GetCheckdigitISBN13, cleanedInput);
            }
            else if (CheckFormatISBN9(cleanedInput))
            {
                IsFormatValid = false;
                IsISBNValid = false;
                ISBNFormat = "ISBN-10";
                FinalISBN = GetFinalISBN(GetCheckdigitISBN10, cleanedInput);
            }
            else if (CheckFormatISBN12(cleanedInput))
            {
                IsFormatValid = false;
                IsISBNValid = false;
                ISBNFormat = "ISBN-13";
                FinalISBN = GetFinalISBN(GetCheckdigitISBN13, cleanedInput);
            }
            else
            {
                IsFormatValid = false;
                IsISBNValid = false;
                ISBNFormat = string.Empty;
                FinalISBN = string.Empty;
            }
        }

        private string CleanInput(string input)
        {
            //Replace multiple consecutive spaces in a string with a single space
            string cleanedInput = Regex.Replace(input, @"\s+", " ").Trim();
            return cleanedInput;
        }

        private bool CheckFormatISBN10(string input)
        {
            return isbn10FormatRegex.IsMatch(input);
        }

        private bool CheckFormatISBN13(string input)
        {
            return isbn13FormatRegex.IsMatch(input);
        }

        private bool CheckFormatISBN9(string input)
        {
            return isbn9FormatRegex.IsMatch(input);
        }

        private bool CheckFormatISBN12(string input)
        {
            return isbn12FormatRegex.IsMatch(input);
        }

        private bool CheckValidityISBN10(string input)
        {

            char lastDigit = GetCheckdigitISBN10(input);

            return lastDigit == input[input.ToString().Length - 1];

        }

        private bool CheckValidityISBN13(string input)
        {

            char lastDigit = GetCheckdigitISBN13(input);

            return lastDigit == input[input.ToString().Length - 1];
        }

        private char GetCheckdigitISBN10(string input)
        {
            input = Regex.Replace(input, @"[\s-]", "");
            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                sum += int.Parse(input[i].ToString()) * (10 - i);
            }

            int lastDigit = 11 - (sum % 11); 

            //check if the last character if it's 10 then convert it to 'X' 
            return lastDigit == 10 ? 'X' : char.Parse(lastDigit.ToString());
        }

        private char GetCheckdigitISBN13(string input)
        {
            input = Regex.Replace(input, @"[\s-]", "");
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                sum += (i % 2 == 0) ? int.Parse(input[i].ToString()) : int.Parse(input[i].ToString()) * 3;
            }

            int lastDigit = 10 - (sum % 10);
            return char.Parse(lastDigit.ToString());
        }

        private string GetFinalISBN(Func<string, char> CalCheckdigit, string input)
        {
            char checkDigit = CalCheckdigit(input);
            string restult = string.Empty;
            if (input.Contains('-'))
            {
                restult = input + (input.EndsWith('-') ? "" : "-") + checkDigit.ToString();
            }
            else if (input.Contains(' '))
            {
                restult = input + " " + checkDigit.ToString();
            }
            else
                restult = input + checkDigit.ToString();
            
            return restult;
        }
    }
}
