using System;
using System.Web.UI.WebControls;

namespace AIT_Research
{
    public partial class MemebrQuestion : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            queslbl.Text = "Would You Like to Register as a member";
            if (!IsPostBack)
            {
                LoadOptions();
            }
           
        }

        protected void RadioButtonListOptions_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void LoadOptions()
        {
            RadioButtonListOptions.Items.Clear();
            RadioButtonListOptions.Items.Add(new ListItem("Yes", "yes"));
            RadioButtonListOptions.Items.Add(new ListItem("No", "no"));
        }

        protected void btnNext_Click(object sender, EventArgs e)
        {
            if (RadioButtonListOptions.SelectedIndex >= 0)
            {
                string selectedValue = RadioButtonListOptions.SelectedItem.Text;

                if (selectedValue.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect("Register.aspx");
                }
                else
                {
                    RadioButtonListOptions.Visible=false;
                    queslbl.Text = "Thank you for the survey!";
                    btnNext.Text = "Finish";


                }
            }
            else
            {
                Warning.Text = "Please select an option.";
                Warning.Visible = true;
            }
        }

        
    }
}
