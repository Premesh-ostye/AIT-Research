<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="staffSearch.aspx.cs" Inherits="AIT_Research.staffSearch" %>

<!DOCTYPE html>
<html>
<head>
    <title>Search Respondents</title>
        <link href="Content/styles.css" rel="stylesheet" type="text/css" />

   
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="card search-card">
                <h1>Search Respondents</h1>

                <div class="textFieldGRP">
                    <input type="text" class="text-answer" placeholder="Given Name (eg: John)" />
                    <input type="text" class="text-answer" placeholder="Last Name (eg: Doe)" />
                </div>

                <div class="inline-group" style="margin-top: 15px;">
                    <select class="text-answer">
                        <option>Age Range</option>
                        <option>18–25</option>
                        <option>26–35</option>
                        <option>36–50</option>
                        <option>50+</option>
                    </select>

                    <select class="text-answer">
                        <option>State</option>
                        <option>NSW</option>
                        <option>VIC</option>
                        <option>QLD</option>
                        <option>WA</option>
                    </select>
                </div>

                <div class="inline-group" style="margin-top: 15px;">
                    <label><input type="checkbox" /> Westpac</label>
                    <label><input type="checkbox" /> ANZ</label>
                </div>

                <div class="inline-group" style="margin-top: 20px;">
                    <button type="button" class="btn">Clear</button>
                    <button type="submit" class="btn">Submit</button>
                </div>

                <h2 style="margin-top: 30px;">Matching Respondents</h2>

                <div class="grid-wrap">
                    <table class="grid-table">
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Age</th>
                                <th>Gender</th>
                                <th>State</th>
                                <th>Banks</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td>John Doe</td>
                                <td>30</td>
                                <td>Male</td>
                                <td>NSW</td>
                                <td>ANZ</td>
                            </tr>
                            <tr>
                                <td>Mathew Gerald</td>
                                <td>28</td>
                                <td>Male</td>
                                <td>VIC</td>
                                <td>WBC</td>
                            </tr>
                            <tr>
                                <td>Priyanka Gupta</td>
                                <td>45</td>
                                <td>Female</td>
                                <td>QLD</td>
                                <td>Commonwealth</td>
                            </tr>
                            <tr>
                                <td>Ruth Garcia</td>
                                <td>62</td>
                                <td>Female</td>
                                <td>NSW</td>
                                <td>NAB</td>
                            </tr>
                        </tbody>
                    </table>
                </div>

            </div>
        </div>
    </form>
</body>
</html>
