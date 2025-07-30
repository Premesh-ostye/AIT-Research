using System;
using System.Configuration;
using System.Data.SqlClient;

namespace AIT_Research
{
    public partial class Register : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e) {
            
        }

        protected void Submitbtn_Click(object sender, EventArgs e)
        {
            string firstName = inputFN.Text.Trim();
            string lastName = txtLastName.Text.Trim();
            string dob = txtDOB.Text;
            string phone = txtPhone.Text.Trim();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(phone))
            {
                lblMsg.Text = "All fields are required.";
                return;
            }

            string connStr = ConfigurationManager.ConnectionStrings["AITResearchDB"].ConnectionString;


            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Insert the member and get the new RespondentID
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Respondent (GivenName, LastName, DOB, PhoneNumber) OUTPUT INSERTED.RespondentID VALUES (@g, @l, @d, @p)", conn);

                cmd.Parameters.AddWithValue("@g", firstName);
                cmd.Parameters.AddWithValue("@l", lastName);
                cmd.Parameters.AddWithValue("@d", dob);
                cmd.Parameters.AddWithValue("@p", phone);

                int newRespondentID = (int)cmd.ExecuteScalar(); 

                Session["RespondentID"] = newRespondentID; // ✅ Save as int
            }

            Response.Redirect("Home.aspx");
        }

    }
}
