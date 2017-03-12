﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace seng403alarmclock.GUI {
    /// <summary>
    /// A representation of an alarm for the GUI
    /// </summary>
    public class AlarmRow {
        /// <summary>
        /// The height of a row for the alarm
        /// </summary>
        private readonly static int rowHeight = 18;

        /// <summary>
        /// The margin on the button for the row
        /// </summary>
        private readonly static Thickness buttonMargin = new Thickness(68, 0, 0, 0);

        /// <summary>
        /// Modes for the row, this controls the text on the row and the types of events generated by the buttons
        /// </summary>
        private enum ModeType {Cancel, Dismiss};

        /// <summary>
        /// The alarm this row describes, this can be used to tell the source of a row
        /// </summary>
        private Alarm storedAlarm = null;

        /// <summary>
        /// The border that holds contents of the row
        /// </summary>
        private Border element = null;

        /// <summary>
        /// The main grid that holds the structure of the row
        /// </summary>
        private Grid mainGrid = null;

        /// <summary>
        /// The stackpanel that holds all rows, this is only let while the row is rendered
        /// </summary>
        private StackPanel parent = null;

        /// <summary>
        /// The button on the row (this reference is kept so the text can be altered)
        /// </summary>
        private Button button = null;
        private Button editBtn = null;
        /// <summary>
        /// Which mode the row is in, control the types of event generated
        /// </summary>
        private ModeType mode = ModeType.Cancel;
        private TextBlock alarmText;
        #region Setup

        /// <summary>
        /// Creates the xaml structure for the row, but does not attach it to the gui
        /// </summary>
        /// <param name="alarm">
        /// The alarm at the base of the row, used for lookup later
        /// </param>
        public AlarmRow(Alarm alarm) {
            //assign the alarm
            this.storedAlarm = alarm;

            this.build();
        }

        /// <summary>
        /// Creates and attaches this object to reflect the current alarm's state
        /// </summary>
        private void build() {
            int rowCount = 2;

            if (storedAlarm.IsWeekly) {
                rowCount++;
            }

            element = new Border();
            element.BorderThickness = new Thickness(1, 1, 1, 1);
            element.BorderBrush = Brushes.DarkGray;


            //create the base grid
            mainGrid = new Grid();
            element.Child = mainGrid;
            mainGrid.Height = rowHeight * rowCount;
            //add rows to the base grid
            for (int i = 0; i < rowCount; i++) {
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(1, GridUnitType.Star);
                mainGrid.RowDefinitions.Add(row);
            }

            //add the rows
            this.CreateTopRow();
           
            //we need to add the weekly row if the alarm is a weekly one, and a repeat row if the alarm repeats
            //this indexes that these things go at depends on each value
            if (storedAlarm.IsWeekly) {
                this.CreateWeekRow(1);
                this.CreateRepeatRow(2);
            } else {
                this.CreateRepeatRow(1);
            }
        }

        /// <summary>
        /// Creates the WPF elements for displaying the top row
        /// </summary>
        private void CreateTopRow() {
            //create the top row grid, set it to row 0 and add it to the main grid
            Grid topRowGrid = new Grid();
            Grid.SetRow(topRowGrid, 0);
            mainGrid.Children.Add(topRowGrid);
            //add columns to the base grid
            for (int i = 0; i < 2; i++) {
                ColumnDefinition col = new ColumnDefinition();
                col.Width = new GridLength(1, GridUnitType.Star);
                topRowGrid.ColumnDefinitions.Add(col);
            }

            //set the thrid col to only take up as much room as button width
            ColumnDefinition col1 = new ColumnDefinition();
            col1.Width = new GridLength(1, GridUnitType.Auto);
            topRowGrid.ColumnDefinitions.Add(col1);

            //create and attach the textbox
            alarmText = new TextBlock();
            Grid.SetColumn(alarmText, 0);
            alarmText.TextAlignment = TextAlignment.Center;
            alarmText.Text = storedAlarm.GetAlarmTime().ToShortTimeString();
            topRowGrid.Children.Add(alarmText);

            //create and attach the button
            editBtn= new Button();
            editBtn.Background = null;
            editBtn.BorderBrush = null;
            Grid.SetColumn(editBtn, 2);
            editBtn.FontFamily = new FontFamily("Segoe UI Symbol");
            //editBtn.Width = 20;
            editBtn.Content = '\uE104';
            button = new Button();
            Grid.SetColumn(button, 1);
            button.Click += ButtonClick; //!this assigns the event handler
            editBtn.Click += EditBtnClick;
            button.BorderThickness = new Thickness(1, 1, 1, 1);
            button.BorderBrush = Brushes.DarkGray;
            topRowGrid.Children.Add(button);
            topRowGrid.Children.Add(editBtn);
            //set the button to the correct mode (as a default)
            if(storedAlarm.IsRinging) {
                this.SetDismiss();
            } else {
                this.SetCancel();
            }
        }

        

        /// <summary>
        /// Adds the elements for the middle row
        /// </summary>
        /// <param name="level">
        /// The index of the row to put this row in
        /// </param>
        private void CreateWeekRow(int level) {
            //create the middle grid, assign it to row 1 and add it to the grid
            Grid weekRowGrid = new Grid();
            Grid.SetRow(weekRowGrid, level);
            mainGrid.Children.Add(weekRowGrid);
            //add columns to the week grid
            for (int i = 0; i < 7; i++) {
                ColumnDefinition col = new ColumnDefinition();
                col.Width = new GridLength(1, GridUnitType.Star);
                weekRowGrid.ColumnDefinitions.Add(col);
            }

           
            //create a lookup table of DayOfWeek => DisplayName
            Dictionary<DayOfWeek, string> daysOfWeekToDisplayValues = new Dictionary<DayOfWeek, string>();
            daysOfWeekToDisplayValues.Add(DayOfWeek.Sunday, "U");
            daysOfWeekToDisplayValues.Add(DayOfWeek.Monday, "M");
            daysOfWeekToDisplayValues.Add(DayOfWeek.Tuesday, "T");
            daysOfWeekToDisplayValues.Add(DayOfWeek.Wednesday, "W");
            daysOfWeekToDisplayValues.Add(DayOfWeek.Thursday, "R");
            daysOfWeekToDisplayValues.Add(DayOfWeek.Friday, "F");
            daysOfWeekToDisplayValues.Add(DayOfWeek.Saturday, "S");

            //get the days of the week this alarm triggers on
            List<DayOfWeek> runsOn = this.storedAlarm.GetWeekdays();

            //iterate over the display values, create a textblock and colour it if it is in the runsOn list 
            int dayTextBlockColumnNumber = 0;
            foreach (KeyValuePair<DayOfWeek, string> entry in daysOfWeekToDisplayValues) {
                TextBlock dayOfWeek = new TextBlock();
                dayOfWeek.Text = entry.Value;
                dayOfWeek.TextAlignment = TextAlignment.Center;
                Grid.SetColumn(dayOfWeek, dayTextBlockColumnNumber);
                weekRowGrid.Children.Add(dayOfWeek);

                if(runsOn.Contains(entry.Key)) {
                    dayOfWeek.Background = Brushes.LightGray;
                } else {
                    dayOfWeek.Background = Brushes.White;
                }


                dayTextBlockColumnNumber++;
            }
        }

        /// <summary>
        /// Creates the elements for the bottom row, only used for repeating alarms
        /// </summary>
        /// <param name="level">
        /// The index of the row to put this row at
        /// </param>
        private void CreateRepeatRow(int level) {
            //create the middle grid, assign it to row 1 and add it to the grid
            Grid repeatRowGrid = new Grid();
            Grid.SetRow(repeatRowGrid, level);
            mainGrid.Children.Add(repeatRowGrid);
            //add column to the repeat grid
           
            ColumnDefinition col = new ColumnDefinition();
            col.Width = new GridLength(1, GridUnitType.Star);
            repeatRowGrid.ColumnDefinitions.Add(col);

            TextBlock repeat = new TextBlock();
            Grid.SetColumn(repeat, level);
            repeatRowGrid.Children.Add(repeat);

            repeat.TextAlignment = TextAlignment.Center;
            if (!this.storedAlarm.IsRepeating) {
                repeat.Text = "Does not repeat";
            } else if(this.storedAlarm.IsWeekly) {
                repeat.Text = "Repeats weekly";
            } else {
                repeat.Text = "Repeats daily";
            }   
        }

        #endregion        private ModeType mode = ModeType.Cancel;

       

        /// <summary>
        /// Event triggered by the button on a row
        /// </summary>
        /// <param name="sender">?</param>
        /// <param name="e">?</param>
        private void ButtonClick(object sender, RoutedEventArgs e) {
            switch(this.mode) {
                case ModeType.Cancel:
                    GuiEventCaller.GetCaller().NotifyCancel(this.storedAlarm);
                    break;
                case ModeType.Dismiss:
                    GuiEventCaller.GetCaller().NotifyDismiss(this.storedAlarm);
                    break;
                default:
                    throw new NotImplementedException("Invalid ModeType");
            } 
        }
       
        private void EditBtnClick(object sender, RoutedEventArgs e)
        {
            EditAlarmWindow eaw = new EditAlarmWindow(200, 200, 500, this.storedAlarm);
            eaw.Show();
        }

        /// <summary>
        /// Adds the row onto the gui
        /// </summary>
        /// <param name="parent">The stack panel from the gui that holds the rows</param>
        public void AddToGUI(StackPanel parent) {
            this.parent = parent;
            parent.Children.Add(element);
        }

        /// <summary>
        /// Removes the row from the gui
        /// </summary>
        public void RemoveFromGUI() {
            parent.Children.Remove(element);
            this.parent = null;
        }

        /// <summary>
        /// Changes the text on the alarm button
        /// </summary>
        /// <param name="buttonText"></param>
        private void AlterText(string buttonText) {
            button.Content = buttonText;
        }

        /// <summary>
        /// Gets the alarm that this row represents
        /// </summary>
        /// <returns>The alarm used to construct the object</returns>
        public Alarm getAlarm() {
            return this.storedAlarm;
        }


        /// <summary>
        /// Set the mode of the row to "Cancel", so it will generate cancel events
        /// </summary>
        public void SetCancel() {
            this.mode = ModeType.Cancel;
            this.AlterText("Cancel");
        }

        /// <summary>
        /// Sets the mode of the row to "Dismiss", so it will generate dismiss events
        /// </summary>
        public void SetDismiss() {
            this.mode = ModeType.Dismiss;
            this.AlterText("Dismiss");
        }

        /// <summary>
        /// Recheck the alarm attributes to change the GUI appropriately
        /// </summary>
        public void Update() {
            if(this.storedAlarm.IsRinging) {
                this.SetDismiss();
            } else {
                this.SetCancel();
            }
        }

        public void UpdateAlarm()
        {
            // this.build();
            //RemoveFromGUI();
            //AddToGUI();
            alarmText.Text = storedAlarm.GetAlarmTime().ToShortTimeString();
        }
    }
}


