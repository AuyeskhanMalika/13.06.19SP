using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using FluentScheduler;
using System.Windows.Threading;
using System.ComponentModel;

namespace _13._06._19SP
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Service _appService;

        public MainWindow()
        {
            InitializeComponent();

            _appService = new Service();

            List<Types> periodTypes = Enum.GetValues(typeof(Types)).Cast<Types>().ToList();
            TypeComboBox.ItemsSource = periodTypes;
            TypeComboBox.SelectedValue = Types.Day;

            startDate.SelectedDate = DateTime.Now;
            endDate.SelectedDate = DateTime.Now.AddDays(1);


            startTime.Text = DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString();
            endTime.Text = DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString();

        }

        private void OpenExecute(object sender, ExecutedRoutedEventArgs e)
        {
            Show();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            base.OnClosing(e);

        }

        private async void SendEmailAsync()
        {
            string emailText = new TextRange(richTextBoxMessage.Document.ContentStart, richTextBoxMessage.Document.ContentEnd).Text;
            var result = await _appService.SendEmailAsync("malika2711@mail.ru", "auyesm", toTextBox.Text, themeText.Text, emailText);

            MessageBox.Show(result);
        }

        private void SendButtonClick(object sender, RoutedEventArgs e)
        {
            if (toTextBox.Text == string.Empty || themeText.Text == string.Empty)
            {
                MessageBox.Show("Fill in all fields to send a message!");
                return;
            }

            if (TypeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Select the period of sending messages!");
                return;
            }

            if (!TimeSpan.TryParse(startTime.Text, out TimeSpan timeStart))
            {
                MessageBox.Show("Start time is not filled correctly!");
                return;
            }

            if (!TimeSpan.TryParse(startTime.Text, out TimeSpan timeEnd))
            {
                MessageBox.Show("End time is not correct!");
                return;
            }

            if (startDate.SelectedDate.Value.AddHours(timeStart.Hours).AddMinutes(timeStart.Minutes) < DateTime.Now)
            {
                MessageBox.Show("Date of starting the operation of sending a message should not be less than the current date!");
                return;
            }

            if (endDate.SelectedDate.Value.AddHours(timeEnd.Hours).AddMinutes(timeEnd.Minutes) < startDate.SelectedDate.Value.AddHours(timeStart.Hours).AddMinutes(timeStart.Minutes))
            {
                MessageBox.Show("The date of the end of the message sending operation should not be less than the start date!");
                return;
            }

            const int INTERVAL = 1;
            var periodType = (Types)TypeComboBox.SelectedValue;

            string jobName = "";

            if (periodType == Types.Day)
            {
                jobName = "DayJob";
                JobManager.AddJob(() => Dispatcher.BeginInvoke(new Action(SendEmailAsync), DispatcherPriority.Background), (schedule) => schedule.WithName(jobName).ToRunEvery(INTERVAL).Days().At(timeStart.Hours, timeStart.Minutes));
            }
            else if (periodType == Types.Week)
            {
                jobName = "WeekJob";
                JobManager.AddJob(() => Dispatcher.BeginInvoke(new Action(SendEmailAsync), DispatcherPriority.Background), (schedule) => schedule.WithName(jobName).ToRunEvery(INTERVAL).Weeks().On(DayOfWeek.Monday).At(timeStart.Hours, timeStart.Minutes));
            }
            else if (periodType == Types.Month)
            {
                jobName = "MonthJob";
                JobManager.AddJob(() => Dispatcher.BeginInvoke(new Action(SendEmailAsync), DispatcherPriority.Background), (schedule) => schedule.WithName(jobName).ToRunEvery(INTERVAL).Months().OnTheFirst(DayOfWeek.Monday).At(timeStart.Hours, timeStart.Minutes));
            }
            else if (periodType == Types.Year)
            {
                jobName = "YearJob";
                JobManager.AddJob(() => Dispatcher.BeginInvoke(new Action(SendEmailAsync), DispatcherPriority.Background), (schedule) => schedule.WithName(jobName).ToRunEvery(INTERVAL).Years().On(1).At(timeStart.Hours, timeStart.Minutes));
            }

            if (startDate.DisplayDate == DateTime.Now.Date)
            {
                JobManager.Start();
            }

            if (endDate.SelectedDate.Value.AddHours(timeEnd.Hours).AddMinutes(timeEnd.Minutes) == DateTime.Now.Date)
            {
                JobManager.Stop();
                JobManager.RemoveJob(jobName);
            }
        }

        private void AddOnceInJob(DateTime operationStartDate, TimeSpan timeExecute, string jobName, Action action)
        {
            TimeSpan intervalTime = operationStartDate.AddHours(timeExecute.Hours).AddMinutes(timeExecute.Minutes) - DateTime.Now;
            int intervalCountSeconds = (int)intervalTime.TotalSeconds;

            JobManager.AddJob(() => Dispatcher.BeginInvoke(new Action(action), DispatcherPriority.Background), (schedule) => schedule.WithName(jobName).ToRunOnceIn(intervalCountSeconds).Seconds());

            if (operationStartDate.AddHours(timeExecute.Hours).AddMinutes(timeExecute.Minutes) == DateTime.Now)
            {
                JobManager.Stop();
                JobManager.RemoveJob(jobName);
            }
        }

        private async void MoveDirectoryAsync()
        {
            var result = await _appService.MoveCatalog(fromPathDirectory.Text, toPathDirectory.Text);
            MessageBox.Show(result);
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (fromPathDirectory.Text == string.Empty || toPathDirectory.Text == string.Empty)
            {
                MessageBox.Show("Fill in all fields for the directory movement.!");
                return;
            }

            if (dateToMove.SelectedDate.Value == null || !TimeSpan.TryParse(timeToMove.Text, out TimeSpan timeExecute))
            {
                MessageBox.Show("The execution time is not correct!");
                return;
            }

            if (dateToMove.SelectedDate.Value.AddHours(timeExecute.Hours).AddMinutes(timeExecute.Minutes) < DateTime.Now)
            {
                MessageBox.Show("The start date of the directory transfer operation must not be less than the current date.!");
                return;
            }

            AddOnceInJob(dateToMove.SelectedDate.Value, timeExecute, "moveFile", MoveDirectoryAsync);
        }

        private async void DownloadFileAsync()
        {
            var result = await _appService.DownloadFile(fromPathDownload.Text, toPathDownload.Text);
            MessageBox.Show(result);
        }



        private void EnableSendMailMenu(object sender, RoutedEventArgs e)
        {
            mailGrid.IsEnabled = true;
            mailDateGrid.IsEnabled = true;
            moveGrid.IsEnabled = false;
        }

        private void EnableMoveDirectoryMenu(object sender, RoutedEventArgs e)
        {
            moveGrid.IsEnabled = true;
            mailGrid.IsEnabled = false;
            mailDateGrid.IsEnabled = false;
        }

        private void EnableDownloadFileMenu(object sender, RoutedEventArgs e)
        {
            moveGrid.IsEnabled = false;
            mailGrid.IsEnabled = false;
            mailDateGrid.IsEnabled = false;
        }

        private void DownloadFileButtonClick(object sender, RoutedEventArgs e)
        {
            if (fromPathDownload.Text == string.Empty || toPathDownload.Text == string.Empty)
            {
                MessageBox.Show("Fill in all the fields to download the file!");
                return;
            }

            if (dateToDownload.SelectedDate.Value == null || !TimeSpan.TryParse(timeToDownload.Text, out TimeSpan timeExecute))
            {
                MessageBox.Show("The execution time is not correct!");
                return;
            }

            if (dateToDownload.SelectedDate.Value.AddHours(timeExecute.Hours).AddMinutes(timeExecute.Minutes) < DateTime.Now)
            {
                MessageBox.Show("The start date of the file download operation should not be less than the current date!");
                return;
            }

            AddOnceInJob(dateToDownload.SelectedDate.Value, timeExecute, "downloadFile", DownloadFileAsync);
        }
    }
}