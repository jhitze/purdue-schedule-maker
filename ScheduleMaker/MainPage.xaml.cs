using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml;
using System.Windows.Browser;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Threading;

namespace ScheduleMaker
{
    public partial class MainPage : UserControl
    {
        List<Class> classes;
        private int classesIndex = 0;
        List<Course> possibleSections;
        int totalClassCount = 0;
        List<List<Course>> confirmedSections;

        public MainPage()
        {
            InitializeComponent();
            btnAddToList.IsEnabled = false;
            btnRemoveFromList.IsEnabled = false;
            btnSubmit.IsEnabled = false;
            confirmedSections = new List<List<Course>>();
            lstExclude.Visibility = System.Windows.Visibility.Collapsed;
            txtExclude.Visibility = System.Windows.Visibility.Collapsed;
            btnExclude.Visibility = System.Windows.Visibility.Collapsed;
            lblExcludeInfo.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void TextBox_GotFocus_1(object sender, RoutedEventArgs e)
        {
            if (courseInput.Text == "Search courses...")
            {
                courseInput.Text = "";
            }
        }

        private void courseInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //Make sure there's a chance for real input
                if (courseInput.Text.Trim() == "") { courseInput.Text = ""; return; }
                //Init data
                classes = new List<Class>();
                classesIndex = 0;
                classList.Items.Clear();
                btnAddToList.IsEnabled = false;
                //Clear entry
                classInfoBox.Text = "";
                //Figure out what department we're looking for
                string input = courseInput.Text;
                //Clear the input box
                courseInput.Text = "";
                int position = -1;                   //The position of where the letters end and numbers start
                input = input.Replace(" ", "");      //Make it fit the format we're expecting -- without spaces
                foreach (char thing in input)
                {
                    position++;
                    //Attempt to convert each character to a number. If it fails, it's a letter.
                    try
                    {
                        Convert.ToInt32(thing.ToString());
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
                if (position == 1) position++;
                string department = input.Substring(0, position).ToLower();
                string number;
                if (department.Length + 1 == input.Length) number = "";
                else number = input.Substring(position);
                //Perform search
                XmlReader reader = XmlReader.Create("classdata.xml");
                do
                {
                    //Navigate to the correct department
                    if (!reader.ReadToFollowing("department"))
                    {
                        break;
                    }
                    
                } while (reader.GetAttribute(0).ToLower() != department);

                bool block = false;
                //Read in all the classes in the department
                do
                {
                    reader.Read();
                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "class":
                                    if (number == "" || reader.GetAttribute(0).ToLower().IndexOf(number) >= 0)
                                    {
                                        classes.Add(new Class());
                                        classes[classesIndex] = new Class();
                                        classes[classesIndex].name = reader.GetAttribute(3);
                                        classes[classesIndex].description = reader.GetAttribute(2);
                                        classes[classesIndex].credits = reader.GetAttribute(1);
                                        classes[classesIndex].course = reader.GetAttribute(0);
                                    }
                                    else block = true;
                                    break;
                                case "section":
                                    if (!block)
                                    {
                                        Course course = new Course();

                                        course.parentClass = classes[classesIndex].ToString();
                                        course.availability = reader.GetAttribute("availability");
                                        course.crn = reader.GetAttribute("crn");
                                        course.days = reader.GetAttribute("days");
                                        course.instructor = reader.GetAttribute("instructor");
                                        course.type = reader.GetAttribute("type");
                                        bool linked = reader.GetAttribute("linkdid") != "0";
                                        if (linked)
                                        {
                                            course.linked = linked;
                                            course.linkID = reader.GetAttribute("linkid");
                                            course.linkedToID = reader.GetAttribute("linkedtoid");
                                        }
                                        if (reader.GetAttribute("time") != "TBA")
                                        {
                                            string[] time = reader.GetAttribute("time").Split('-');
                                            course.startTime = time[0].Trim();
                                            course.endTime = time[1].Trim();
                                        }
                                        else
                                        {
                                            course.startTime = "TBA";
                                            course.endTime = "TBA";
                                        }
                                        classes[classesIndex].sections.Add(course);
                                    }
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.Name == "department") break;
                        if (reader.Name == "class")
                        {
                            if (block)
                            {
                                block = false;
                            }
                            else classesIndex++;
                        }
                    }
                } while (!reader.EOF);
                // All the class data is loaded
                foreach (Class entry in classes)
                {
                    classList.Items.Add(entry);
                }
            }
        }

        private void classList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0 && e.AddedItems.Count < 1)
            {
                return;
            }
            Class classToDisplay = (Class)e.AddedItems[0];
            classInfoBox.Text = "";
            classInfoBox.Text += classToDisplay.course + " - " + classToDisplay.name + '\n' + '\n';
            classInfoBox.Text += classToDisplay.description + "\n";
            classInfoBox.Text += classToDisplay.credits;
            btnAddToList.IsEnabled = true;
        }

        //This is the add button
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Class classToMove = (Class)classList.SelectedItem;
            lstFinalClasses.Items.Add(classToMove);
            classList.Items.Remove(classToMove);
            classInfoBox.Text = "";
            btnSubmit.IsEnabled = true;
            
            //If the list is empty, disable the button
            if (classList.SelectedItems.Count < 1) btnAddToList.IsEnabled = false;
        }

        private void lstFinalClasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtExclude.Text = "";
            if (e.RemovedItems.Count > 0 && e.AddedItems.Count < 1)
            {
                if (lstFinalClasses.Items.Count < 1)
                {
                    btnSubmit.IsEnabled = false;
                }
                return;
            }

            Class classToDisplay = (Class)e.AddedItems[0];
            txtFinalInfo.Text = "";
            txtFinalInfo.Text += classToDisplay.course + " - " + classToDisplay.name + '\n' + '\n';
            txtFinalInfo.Text += classToDisplay.description + "\n";
            txtFinalInfo.Text += classToDisplay.credits;
            btnRemoveFromList.IsEnabled = true;
            btnSubmit.IsEnabled = true;

            lstExclude.Items.Clear();
            foreach (Course course in classToDisplay.sections)
            {
                lstExclude.Items.Add(course);
            }
        }

        private void btnRemoveFromList_Click(object sender, RoutedEventArgs e)
        {
            Class classToRemove = (Class)lstFinalClasses.SelectedItem;

            lstFinalClasses.Items.Remove(classToRemove);
            txtFinalInfo.Text = "";
            btnRemoveFromList.IsEnabled = false;
        }

        private bool isBetween(int start, int question, int end)
        {
            if (question >= start || question <= end)
            {
                return true;
            }
            return false;
        }

        private bool doTimesIntersect(String time1, String time2)
        {
            String startTime, endTime = "";
            startTime = time1.Split(" - ".ToCharArray(), StringSplitOptions.None)[0];
            endTime = time1.Split(" - ".ToCharArray(), StringSplitOptions.None)[1];
            startTime.Replace(" ", "");
            endTime.Replace(" ", "");
            string startMeridian = startTime.Substring(startTime.Length - 2, 2);
            string endMeridian = endTime.Substring(endTime.Length - 2, 2);
            startTime = startTime.Substring(0, startTime.Length - 2);
            endTime = endTime.Substring(0, endTime.Length - 2);
            //Get the first part to determine where to start
            string[] splitStartTime = startTime.Split(':');
            string[] splitEndTime = endTime.Split(':');
            int hourStart = Convert.ToInt32(splitStartTime[0]);
            int hourEnd = Convert.ToInt32(splitEndTime[0]);
            int minutesStart = Convert.ToInt32(splitStartTime[1]);
            int minutesEnd = Convert.ToInt32(splitEndTime[1]);
            if (endMeridian == "pm") hourEnd += 12;
            int startValue = (hourStart * 3) + (minutesStart / 10);
            int endValue = (hourEnd * 3) + (minutesEnd / 10);

            String startTime2, endTime2 = "";
            startTime2 = time2.Split(" - ".ToCharArray(), StringSplitOptions.None)[0];
            endTime2 = time2.Split(" - ".ToCharArray(), StringSplitOptions.None)[1];
            startTime2.Replace(" ", "");
            endTime2.Replace(" ", "");
            string startMeridian2 = startTime2.Substring(startTime2.Length - 2, 2);
            string endMeridian2 = endTime2.Substring(endTime2.Length - 2, 2);
            startTime2 = startTime2.Substring(0, startTime2.Length - 2);
            endTime2 = endTime2.Substring(0, endTime2.Length - 2);
            //Get the first part to determine where to start
            string[] splitStartTime2 = startTime2.Split(':');
            string[] splitEndTime2 = endTime2.Split(':');
            int hourStart2 = Convert.ToInt32(splitStartTime2[0]);
            int hourEnd2 = Convert.ToInt32(splitEndTime2[0]);
            int minutesStart2 = Convert.ToInt32(splitStartTime2[1]);
            int minutesEnd2 = Convert.ToInt32(splitEndTime2[1]);
            if (endMeridian2 == "pm") hourEnd2 += 12;
            int startValue2 = (hourStart2 * 3) + (minutesStart2 / 10);
            int endValue2 = (hourEnd2 * 3) + (minutesEnd2 / 10);

            //Check if they intersect
            if (isBetween(startValue, startValue2, endValue)
                || isBetween(startValue, endValue2, endValue)
                || isBetween(startValue2, startValue, endValue2)
                || isBetween(startValue2, endValue, endValue2))
            {
                return true;
            }
            return false;
        }

        private void generateAllBitStrings(object sender, DoWorkEventArgs e)
        {
            int length = Convert.ToInt32(e.Argument as string);
            List<string> results = new List<string>();
            int n = length;
            int[] cutoffs = new int[n+1];
            Array.Clear(cutoffs, 0, n+1);
            int nsquared = (int) Math.Pow(2,n);
            int percentStep = nsquared / 100;
            if (percentStep == 0) percentStep = 1;
            int percentJump = nsquared / percentStep;
            if (percentJump == 0) percentJump = 1;
            else percentJump = 100 / percentJump;
            for (int row = 1; row <= nsquared; row++)
            {
                string bitstring = "";
                for (int column = 1; column <= n; column++)
                {
                    int cutoff = nsquared / (int)Math.Pow(2, column);
                    if (cutoffs[column] == 0)
                    {
                        cutoffs[column] = cutoff;
                    }
                    if ((row % (cutoff * 2)) == 1)
                    {
                        cutoffs[column] = row+(cutoff-1);
                    }
                    bitstring += (row <= cutoffs[column]) ? "1" : "0";
                }
                results.Add(bitstring);
                //After a certain amount of rows, we need to pass the data into a new thread that will evaluate the
                //If our bitstring candidates are good
                if (row % 10000 == 0)
                {
                    BackgroundWorker checkstringsWorker = new BackgroundWorker();
                    checkstringsWorker.WorkerSupportsCancellation = false;
                    checkstringsWorker.WorkerReportsProgress = false;
                    checkstringsWorker.DoWork += new DoWorkEventHandler(checkBitstrings);
                    checkstringsWorker.RunWorkerAsync(results);
                    results = new List<string>();
                }
                if (row % percentStep == 0)
                {
                    //Thread.Sleep(1);
                    //After a certain amount of time, we need to call another background process
                    //for deciding if a class is a possibility or not and then adding it to the GUI
                    this.Dispatcher.BeginInvoke(delegate { progress.Value += percentJump; });
                    (sender as BackgroundWorker).ReportProgress(0, null);
                }
            }
            this.Dispatcher.BeginInvoke(delegate { progress.Value = 100; });
        }

        private void checkBitstrings(object sender, DoWorkEventArgs e)
        {
            foreach (string bitstring in (e.Argument as List<string>))
            {
                List<Course> possibility = new List<Course>();
                int iterator = 0;
                foreach (char bit in bitstring)
                {
                    if (bit == '1')
                    {
                        possibility.Add(possibleSections[iterator]);
                    }
                    iterator++;
                }
                //If the combination works, add it to the list of possibilities
                if (combinationSuccessful(possibility))
                {
                    lock (confirmedSections)
                    {
                        confirmedSections.Add(possibility);
                    }
                }
            }
        }

        private bool combinationSuccessful(List<Course> possibilities)
        {
            //First check that all classes are represented.
            int classCount = totalClassCount;
            Dictionary<string, List<Course>> classesDict = new Dictionary<string, List<Course>>();
            foreach (Course current in possibilities)
            {
                string parent = current.parentClass;
                if (!(classesDict.ContainsKey(parent)))
                {
                    List<Course> toAdd = new List<Course>();
                    toAdd.Add(current);
                    classesDict.Add(parent, toAdd);
                }
                else
                {
                    List<Course> courses;
                    classesDict.TryGetValue(current.parentClass, out courses);
                    courses.Add(current);
                }
            }
            if (classesDict.Keys.Count != classCount)
            {
                //If the number of keys are not equal to the number of classes
                return false;
            }
            else return true;
            //Add a working possibility to a global list, then periodically flush the list to the GUI
        }

        private void reportProgress(object sender, ProgressChangedEventArgs e)
        {
            List<List<Course>> reportingList = new List<List<Course>>();
            lock (confirmedSections)
            {
                foreach (List<Course> classGroup in confirmedSections)
                {
                    txtFinalInfo.Text += "Found\n";
                    reportingList.Add(classGroup);
                }
                confirmedSections = new List<List<Course>>();
            }
            foreach (List<Course> classGroup in reportingList)
            {
                
            }
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            List<Class> listCombinations = new List<Class>();
            possibleSections = new List<Course>();               //All of the possible courses
            progress.Value = 0;                                 //Reset the progress bar
            //Start finding bitstrings in the background
            BackgroundWorker bitstringworker = new BackgroundWorker();
            bitstringworker.WorkerSupportsCancellation = false;
            bitstringworker.WorkerReportsProgress = true;
            bitstringworker.DoWork += new DoWorkEventHandler(generateAllBitStrings);
            bitstringworker.ProgressChanged += new ProgressChangedEventHandler(reportProgress);
            int totalClasses = 0;
            //Count how many courses there are so we know how many bitstrings to generate
            foreach (Class finalClass in lstFinalClasses.Items)
            {
                foreach (Course course in finalClass.sections)
                {
                    //if course is not in exlusions:
                    possibleSections.Add(course);
                }
                totalClasses += finalClass.sections.Count;
            }
            //Start generating bitstrings
            bitstringworker.RunWorkerAsync("" + totalClasses);
            totalClassCount = totalClasses;
            txtFinalInfo.Text = "";
            return;
            foreach (Class finalClass in lstFinalClasses.Items)
            {
                count++;
                TabItem tab = new TabItem();
                tab.Header = ""+count;
                Grid grid = new Grid();
                //Draw horizontal lines
                for (int i = 0; i < 80; i++)
                {
                    Line gridLines = new Line();
                    gridLines.X1 = 40;
                    gridLines.X2 = tabBase.ActualWidth - 89;
                    gridLines.Y1 = 60 + 6.7*i;
                    gridLines.Y2 = 60 + 6.7*i;
                    if (i % 3 == 0) gridLines.Stroke = new SolidColorBrush(Colors.DarkGray);
                    else gridLines.Stroke = new SolidColorBrush(Colors.Transparent);
                    gridLines.StrokeThickness = 0.5f;
                    grid.Children.Add(gridLines);
                }
                for (int i = 0; i < 6; i++)
                {
                    Line gridLines = new Line();
                    gridLines.X1 = 40 + ((tabBase.ActualWidth / 6) + .80f) * i;
                    gridLines.X2 = 40 + ((tabBase.ActualWidth / 6) + .80f) * i;
                    gridLines.Y1 = 60;
                    gridLines.Y2 = 583;
                    gridLines.Stroke = new SolidColorBrush(Colors.DarkGray);
                    gridLines.StrokeThickness = 0.5f;
                    grid.Children.Add(gridLines);
                }


                if (finalClass.sections.Count > 0)
                {
                    foreach (Course course in finalClass.sections)
                    {
                        drawSchedule(grid, course.startTime, course.endTime, course.days);
                    }
                }

                tab.Content = grid;
                tabBase.Items.Add(tab);
            }
        }

        private void drawSchedule(Grid grid, string startTime, string endTime, string days)
        {
            //Change "11:30 am" to "11:30"
            txtFinalInfo.Text = "";
            txtFinalInfo.Text += startTime + " - " + endTime + "\n";
            startTime.Replace(" ", "");
            endTime.Replace(" ", "");
            string startMeridian = startTime.Substring(startTime.Length - 2, 2);
            string endMeridian = endTime.Substring(endTime.Length - 2, 2);
            startTime = startTime.Substring(0, startTime.Length - 2);
            endTime = endTime.Substring(0, endTime.Length - 2);
            //Get the first part to determine where to start
            string[] splitStartTime = startTime.Split(':');
            string[] splitEndTime = endTime.Split(':');
            int hourStart = Convert.ToInt32(splitStartTime[0]);
            int hourEnd = Convert.ToInt32(splitEndTime[0]);

            int positionStart = 3;
            //Make our clock a 24 hour system
            if (startMeridian == "pm") hourStart += 12;
            //The hour of the first class is at 7 -- so 
            hourStart -= 7;
            positionStart += 6 * (hourStart-1);
            txtFinalInfo.Text += hourStart+"\n";

            positionStart += (int)((Math.Floor((Convert.ToDouble(splitStartTime[1]))))/10.0);
            txtFinalInfo.Text += (int)((Math.Floor((Convert.ToDouble(splitStartTime[1])))) / 10.0) + "\n";

            int positionEnd = 3;
            //Make our clock a 24 hour system
            if (endMeridian == "pm") hourEnd += 12;
            //The hour of the first class is at 7
            hourEnd -= 7;
            positionEnd += 6 * (hourEnd-1);
            txtFinalInfo.Text += hourEnd + "\n";

            positionEnd += (int)((Math.Floor((Convert.ToDouble(splitEndTime[1])))) / 10.0);

            days = days.ToLower();
            Canvas canvas = new Canvas();
            foreach (char character in days)
            {
                Rectangle block = new Rectangle();
                canvas.Children.Add(block);
                block.Fill = new SolidColorBrush(Colors.Blue);
                block.Stroke = new SolidColorBrush(Colors.Black);
                block.StrokeThickness = 2.0d;
                block.RadiusX = block.RadiusY = 5.0d;
                block.Width = ((tabBase.ActualWidth / 6) + .80f);
                block.Height = (positionEnd - positionStart) * 6.7;
                switch (character)
                {
                    case 'm':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d);
                        break;
                    case 't':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 1);
                        break;
                    case 'w':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 2);
                        break;
                    case 'r':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 3);
                        break;
                    case 'f':
                        block.SetValue(Canvas.TopProperty, 60d + (Double)(6.7 * positionStart));
                        block.SetValue(Canvas.LeftProperty, 40d + ((tabBase.ActualWidth / 6) + .80f) * 4);
                        break;
                }
            }
            grid.Children.Add(canvas);
        }

        private void tabBase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count < 1) return;
            if (e.RemovedItems[0] == mainTab)
            {
            }
            else if (e.AddedItems[0] == mainTab)
            {
                ((Grid)((TabItem)e.RemovedItems[0]).Content).Children.Remove(grdSchedule);
                return;
            }
            ((Grid)((TabItem)e.RemovedItems[0]).Content).Children.Remove(grdSchedule);
            ((Grid)((TabItem)e.AddedItems[0]).Content).Children.Add(grdSchedule);
        }

        private void chkExclude_Checked(object sender, RoutedEventArgs e)
        {
            lstExclude.Visibility = System.Windows.Visibility.Visible;
            txtExclude.Visibility = System.Windows.Visibility.Visible;
            btnExclude.Visibility = System.Windows.Visibility.Visible;
            lblExcludeInfo.Visibility = System.Windows.Visibility.Visible;
        }

        private void chkExclude_Unchecked(object sender, RoutedEventArgs e)
        {
            lstExclude.Visibility = System.Windows.Visibility.Collapsed;
            txtExclude.Visibility = System.Windows.Visibility.Collapsed;
            btnExclude.Visibility = System.Windows.Visibility.Collapsed;
            lblExcludeInfo.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void lstExclude_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0 && e.AddedItems.Count < 1)
            {
                return;
            }
            Course course = (Course)e.AddedItems[0];
            txtExclude.Text = "";
            txtExclude.Text += "CRN: " + course.crn + "\n";
            txtExclude.Text += "Meets on " + course.days + " at " + course.startTime + " - " + course.endTime + "\n";
            txtExclude.Text += "Type: " + course.type + "\n";
            txtExclude.Text += "Instructor: " + course.instructor + "\n";
            txtExclude.Text += "\nAvailability: " + course.availability;
        }
    }
}
