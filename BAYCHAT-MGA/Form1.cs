using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;

namespace BAYCHAT_MGA
{
    public partial class Form1 : Form
    {
        const string API_URL = "https://YOUR_DOMAIN.com/api/v1/";
        const string BOT_NAME = "YOUR_BOT_ACCOUNT";
        const string BOT_PASSWORD = "YOUR_BOT_PWD";

        RestClient client = new RestClient(API_URL);

        //dict for group names/ids
        Dictionary<string, string> groups_and_id = new Dictionary<string, string>();

        //dict for user names/ids
        Dictionary<string, string> users_and_id = new Dictionary<string, string>();

        string AUTH_TOKEN = "";
        string USER_ID = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {   
            client.Authenticator = new SimpleAuthenticator("user", BOT_NAME, "password", BOT_PASSWORD);

            //login to api for auth token
            var request = new RestRequest("login", Method.POST);
            var response = client.Execute(request);

            dynamic content = JsonConvert.DeserializeObject(response.Content);
            var data = content.data;

            //grab auth token and bot id
            string auth = data.authToken.ToString();
            string userId = data.userId.ToString();

            AUTH_TOKEN = auth;
            USER_ID = userId;

            //grab list of groups
            var groupRequest = new RestRequest("groups.listAll", Method.GET);
            groupRequest.AddHeader("X-Auth-Token", auth);
            groupRequest.AddHeader("X-User-Id", userId);
            groupRequest.AddHeader("Content-Type", "application/json");

            var groupResponse = client.Execute(groupRequest);

            dynamic groupContent = JsonConvert.DeserializeObject(groupResponse.Content);

            var groups = groupContent.groups;

            //pull group name and group ID, place into group dict and add group name to check box list
            foreach(var chatGroup in groups)
            {
                string name = chatGroup.name.ToString();
                string groupId = chatGroup._id;
                groups_and_id.Add(name, groupId);
                channelCheckListBox.Items.Add(name);
            }

            //pull user list
            var userRequest = new RestRequest("users.list", Method.GET);
            userRequest.AddHeader("X-Auth-Token", auth);
            userRequest.AddHeader("X-User-Id", userId);
            userRequest.AddHeader("Content-Type", "application/json");

            var userRespone = client.Execute(userRequest);

            dynamic userContent = JsonConvert.DeserializeObject(userRespone.Content);

            var users = userContent.users;

            //pull user names and ids, add to user checkbox list and dict
            foreach(var user in users)
            {
                string name = user.name.ToString();
                string id = user._id;
                users_and_id.Add(name, id);
                usersCheckListBox.Items.Add(name);
            }
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        void discard_selections()
        {
            for (int i = 0; i < channelCheckListBox.Items.Count; i++)
            {
                channelCheckListBox.SetItemChecked(i, false);
            }

            for (int i = 0; i < usersCheckListBox.Items.Count; i++)
            {
                usersCheckListBox.SetItemChecked(i, false);
            }
        }

        private void discardButton_Click(object sender, EventArgs e)
        {
            discard_selections();
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            //new login
            var loginRequest = new RestRequest("login", Method.POST);
            var loginResponse = client.Execute(loginRequest);

            dynamic loginContent = JsonConvert.DeserializeObject(loginResponse.Content);
            var data = loginContent.data;

            //new auth token
            string auth = data.authToken.ToString();
            string userId = data.userId.ToString();

            AUTH_TOKEN = auth;
            USER_ID = userId;

            //ensure at least 1 user selected
            int userCount = usersCheckListBox.CheckedItems.Count;
            if(userCount == 0)
            {
                MessageBox.Show("Please select at least one user name.");
                return;
            }

            //ensure at least 1 group selected
            int groupCount = channelCheckListBox.CheckedItems.Count;
            if(groupCount == 0)
            {
                MessageBox.Show("Please select at least one group.");
                return;
            }

            //begin collecting users selected into a list
            List<string> users_checked = new List<string>();

            foreach(string user in usersCheckListBox.CheckedItems)
            {
                users_checked.Add(user);
            }

            //iterate over each user
            foreach (string userName in users_checked)
            {
                //user ID for adding to group
                string user_id = users_and_id[userName];

                //iterate over each group selected
                foreach (string groupName in channelCheckListBox.CheckedItems)
                {
                    //for adding user to group
                    string groupId = groups_and_id[groupName];

                    var client = new RestClient(API_URL);
                    var addUserRequest = new RestRequest("groups.invite", Method.POST);

                    addUserRequest.AddHeader("X-Auth-Token", AUTH_TOKEN);
                    addUserRequest.AddHeader("X-User-Id", USER_ID);
                    addUserRequest.AddHeader("Content-Type", "application/json");

                    addUserRequest.AddJsonBody((new { roomId = groupId, userId = user_id }));

                    //no need for return values. it worked or it didnt
                    client.Execute(addUserRequest);
                }
            }

            //some output is nice
            string output = "Added users: \n\n";

            foreach(string user_name in users_checked)
            {
                output += user_name + "\n";
            }

            output += "\nTo Groups: \n\n";
            foreach(string groupName in channelCheckListBox.CheckedItems)
            {
                output += groupName + "\n";
            }

            MessageBox.Show(output);

            //clean up form
            discard_selections();
        }
    }
}
