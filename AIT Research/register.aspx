<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="register.aspx.cs" Inherits="AIT_Research.StaffLogin" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Registration Page</title>
    <link href="Content/styles.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
     <div class="card">
         <h1>Register</h1>
          <div class="textFieldGRP">
              <input id="inputFN" type="text" class="text-answer" placeholder="First Name" />
              <input id="InputLN" type="text" class="text-answer" placeholder="Last Name" />
              <input id="InputDOB" type="date" class="text-answer" placeholder="Date of Birth" />
              <input id="INputPHNumber" type="tel" class="text-answer" placeholder="Phone Number" width="10" />
           </div>
             <asp:Button ID="Submitbtn" runat="server" Text="Submit" CssClass="btn" />
         </div>
    
 </div>
    </form>
</body>
</html>
