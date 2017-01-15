using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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

namespace ThingSpeakExample
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SendDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {                                              
                string request_str = $"https://api.thingspeak.com/update?api_key={SensorKeyTextBox.Text}&field1={VolumeTextBox.Text}&field2={TemperatureTextBox.Text}";

                WebRequest request = WebRequest.Create(request_str);
                request.Method = "GET";

                WebResponse ws = request.GetResponse();
                string response = string.Empty;
                using (System.IO.StreamReader sreader = new System.IO.StreamReader(ws.GetResponseStream()))
                {
                    response = sreader.ReadToEnd();
                }

                if (response != "0")
                    MessageBox.Show($"Request successful. Number of records: {response}");
                else
                    MessageBox.Show("Request failed on server (value has not changed or incorrect).");
            }
            catch (Exception ex)
            {                      
                MessageBox.Show(ex.ToString());
            }

        }

        struct SensorFields
        {
            public double volume, temperature;
        };

        private void GetValuesButton_Click(object sender, RoutedEventArgs e)
        {           
            try
            {                
                string request_str = $"https://api.thingspeak.com/channels/{SensorIdTextBox.Text}/feeds.json?results={RequestedRecordsTextBox.Text}&api_key={SensorKeyTextBox.Text}";

                WebRequest request = WebRequest.Create(request_str);
                request.Method = "GET";

                WebResponse ws = request.GetResponse();
                string response = string.Empty;
                using (System.IO.StreamReader sreader = new System.IO.StreamReader(ws.GetResponseStream()))
                {
                    response = sreader.ReadToEnd();
                }
                              
                ParseAndAnalyzeResponse(response);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        private void ParseAndAnalyzeResponse(string response)
        {
            //parse coordinates       
            string latlon_regex_str = "latitude\":\"(?<latitude>(?:\\d|\\.)*)\",\"longitude\":\"(?<longitude>(?:\\d|\\.)*)\"";

            Regex latlon_regex = new Regex(latlon_regex_str);
            MatchCollection latlon_matches = latlon_regex.Matches(response);
            if (latlon_matches.Count != 1)
            {
                throw new Exception("latlon_regex parse error");
            }
            string sensor_coordinates = $"Coordinates = {latlon_matches[0].Groups["latitude"].Value} : {latlon_matches[0].Groups["longitude"].Value}";

            //parse fields values
            string fields_regex_str = "(\"field1\":\"(?<field1>(?:\\d|\\.)*)\",\"field2\":\"(?<field2>(?:\\d|\\.)*)\")+";

            Regex fields_regex = new Regex(fields_regex_str);
            MatchCollection fields_matches = fields_regex.Matches(response);
            if (fields_matches.Count == 0)
            {
                throw new Exception("fields_regex parse error");
            }

            List<SensorFields> sensor_fields = new List<SensorFields>();

            foreach (Match fields in fields_matches)
            {
                SensorFields fields_struct = new SensorFields();
                fields_struct.volume = Convert.ToDouble(fields.Groups["field1"].Value);
                fields_struct.temperature = Convert.ToDouble(fields.Groups["field2"].Value);
                sensor_fields.Add(fields_struct);
            }

            //Report message box
            string report = $"Server response:\n" + response;

            report += "\nParsed data:\n";
            report += sensor_coordinates + "\n";
            report += "Last values list:\n";
            foreach (SensorFields fields in sensor_fields)
            {
                report += $"Volumme = {fields.volume} Temperature = {fields.temperature}\n";
            }

            //Analyze values
            report += "Sensor state: ";

            if (sensor_fields.Last().temperature > 30 || sensor_fields.Last().volume > 75)
                report += "ALARM! (last temperature > 30 or last volume > 75)\n";
            else
                report += "OK\n";

            MessageBox.Show(report);   
        }
    }
}
