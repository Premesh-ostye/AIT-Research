using System;
using System.Web.UI.WebControls;
using System.Diagnostics;

namespace AIT_Research
{
    public partial class MemebrQuestion : System.Web.UI.Page
    {
        // Handles the page load event to initialize the question and options
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                // Set the question text for the membership prompt
                queslbl.Text = "Would you like to register as a member?";

                // Load options only on initial page load, not on postback
                if (!IsPostBack)
                {
                    LoadOptions();
                }
            }
            catch (Exception ex)
            {
                // Log any unexpected errors and display a user-friendly message
                LogError("Page_Load", ex);
                DisplayError("An error occurred while loading the page. Please try again.");
            }
        }

        // Handles the RadioButtonList selection change event (currently unused)
        protected void RadioButtonListOptions_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Placeholder for future functionality if needed
        }

        // Loads the Yes/No options into the RadioButtonList
        private void LoadOptions()
        {
            try
            {
                // Clear any existing options to prevent duplicates
                RadioButtonListOptions.Items.Clear();

                // Add Yes and No options to the RadioButtonList
                RadioButtonListOptions.Items.Add(new ListItem("Yes", "yes"));
                RadioButtonListOptions.Items.Add(new ListItem("No", "no"));
            }
            catch (Exception ex)
            {
                // Log any errors during option loading and notify the user
                LogError("LoadOptions", ex);
                DisplayError("Error loading options. Please try again.");
            }
        }

        // Handles the Next button click to process user selection
        protected void btnNext_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if the button text is "Finish" to end the session
                if (btnNext.Text == "Finish")
                {
                    // Clear and abandon the session
                    Session.Clear();
                    Session.Abandon();

                    
                }

                // Verify that an option is selected
                if (RadioButtonListOptions.SelectedIndex >= 0)
                {
                    string selectedValue = RadioButtonListOptions.SelectedItem.Text;

                    // Redirect to registration page if user selects "Yes"
                    if (selectedValue.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                    {
                        Response.Redirect("Register.aspx");
                    }
                    // Display thank you message and change button to "Finish" for "No"
                    else
                    {
                        RadioButtonListOptions.Visible = false;
                        queslbl.Text = "Thank you for completing the survey!";
                        btnNext.Text = "Finish";
                    }
                }
                else
                {
                    // Display error if no option is selected
                    DisplayError("Please select an option.");
                }
            }
            catch (Exception ex)
            {
                // Log any errors during button click processing and notify the user
                LogError("btnNext_Click", ex);
                DisplayError("Error processing your selection. Please try again.");
            }
        }

        // Displays an error message to the user
        private void DisplayError(string message)
        {
            Warning.Text = message;
            Warning.Visible = true;
        }

        // Logs errors to the system event log for debugging
        private void LogError(string methodName, Exception ex)
        {
            try
            {
                EventLog.WriteEntry("AIT_Research_MemberQuestion",
                    $"Error in {methodName}: {ex.Message}\nStackTrace: {ex.StackTrace}",
                    EventLogEntryType.Error);
            }
            catch
            {
                
            }
        }
    }
}