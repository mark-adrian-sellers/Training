
    public static class DateHandlerExtensions
    {
        public static int UofIFiscalYear(this DateTime date)
        {
            var year = date.Year;
            var month = date.Month;
            if (month > 6) year++;
            return year;
        }

    }

  
