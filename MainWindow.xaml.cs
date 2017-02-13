using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using LiteDB;
using System.ComponentModel;
using System.Globalization;

namespace Times
{
    public class ProgressForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double progress = (double)value;
            Brush foreground = Brushes.Green;
            
            if (progress >= 40d)
                foreground = Brushes.Green;
            else if (progress >= 20d)
                foreground = Brushes.Yellow;
            else if (progress >= 10d)
                foreground = Brushes.Orange;
            else if (progress <= 10d)
                foreground = Brushes.Red;
            
            return foreground;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private double _value;
        public double Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged("Value");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
        	if (PropertyChanged != null)
            	PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();

            txtDelay.Background = Brushes.Red;
            txtOvertime.Background = Brushes.Green;

            DataContext = this;

            refreshCalendar();
        }

        private void refreshCalendar()
        {
            // Set selected date to current display date
            cldTimes.SelectedDate = cldTimes.DisplayDate;

            // Open database (or create if not exits)
            using (var db = new LiteDatabase(@"MyTimes.db"))
            {
                // Get times collection
                var times = db.GetCollection<Time>("times");

                // Create a new style
                Style st = new Style(typeof(CalendarDayButton));

                // Month Details
                int monthDelay = 0;
                int monthOvertime = 0;
                int monthDifference = 0;

                // Get all results from database
                var results = times.FindAll().Where(x => x.Date.Month == cldTimes.DisplayDate.Month);
                foreach (Time dayTime in results)
                {
                    DataTrigger dataTrigger = new DataTrigger() { Binding = new Binding("Date"), Value = dayTime.Date };
                    if (dayTime.Delay > dayTime.Overtime)
                        dataTrigger.Setters.Add(new Setter(CalendarDayButton.BackgroundProperty, Brushes.Red));
                    else if (dayTime.Delay < dayTime.Overtime)
                        dataTrigger.Setters.Add(new Setter(CalendarDayButton.BackgroundProperty, Brushes.Green));

                    // Add to style triggers
                    st.Triggers.Add(dataTrigger);

                    // Add to month details
                    monthDelay += dayTime.Delay;
                    monthOvertime += dayTime.Overtime;
                }

                monthDifference = monthOvertime - monthDelay;
                if (monthOvertime < monthDelay) {
                	Value = 60 - Math.Abs(monthDifference);
                } else {
                	Value = 60;
                }

                // Set the style
                cldTimes.CalendarDayButtonStyle = st;

                // Set the text blocks with month details
                txbMonthDelay.Text = monthDelay.ToString();
                txbMonthOvertime.Text = monthOvertime.ToString();
                if (monthDifference >= 0)
                    txbMonthDifference.Foreground = Brushes.Green;
                else
                    txbMonthDifference.Foreground = Brushes.Red;
                txbMonthDifference.Text = monthDifference.ToString();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Open database (or create if not exits)
            using (var db = new LiteDatabase(@"MyTimes.db"))
            {
                // Get times collection
                var times = db.GetCollection<Time>("times");

                // Find times for selected day
                var result = times.FindOne(x => x.Date == cldTimes.SelectedDate);
                if (result == null)
                {
                    // Insert a new record
                    int delay = 0;
                    Int32.TryParse(txtDelay.Text, out delay);
                    int overtime = 0;
                    Int32.TryParse(txtOvertime.Text, out overtime);

                    // Create your new customer instance
                    var time = new Time
                    {
                        Date = cldTimes.SelectedDate.Value,
                        Delay = delay,
                        Overtime = overtime
                    };

                    // Insert new time document (Id will be auto-incremented)
                    times.Insert(time);

                    // Clean text boxes
                    txtDelay.Clear();
                    txtOvertime.Clear();

                    // Refresh Calendar
                    refreshCalendar();
                }
                else
                {
                    // Update record
                    int delay = 0;
                    Int32.TryParse(txtDelay.Text, out delay);
                    int overtime = 0;
                    Int32.TryParse(txtOvertime.Text, out overtime);

                    result.Overtime = overtime;
                    result.Delay = delay;

                    // Update the times
                    times.Update(result);

                    // Refresh Calendar
                    refreshCalendar();
                }
            }
        }

        private void txtOvertime_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]"); //regex that matches disallowed text
            if (regex.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private void txtDelay_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]"); //regex that matches disallowed text
            if (regex.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private void cldTimes_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            // Open database (or create if not exits)
            using (var db = new LiteDatabase(@"MyTimes.db"))
            {
                // Get times collection
                var times = db.GetCollection<Time>("times");

                // Find times for selected day
                var result = times.FindOne(x => x.Date == cldTimes.SelectedDate);
                if (result != null)
                {
                    if (result.Delay != 0)
                        txtDelay.Text = result.Delay.ToString();
                    else
                        txtDelay.Clear();
                    if (result.Overtime != 0)
                        txtOvertime.Text = result.Overtime.ToString();
                    else
                        txtOvertime.Clear();
                }
                else
                {
                    // Clean text boxes
                    txtDelay.Clear();
                    txtOvertime.Clear();
                }
            }
        }

        private void cldTimes_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Fix problem with double click needed outside calendar if a day in 
            // calendar is selected
            if (Mouse.Captured is CalendarItem)
            {
                Mouse.Capture(null);
            }
        }

        private void cldTimes_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            refreshCalendar();
        }
    }
}
