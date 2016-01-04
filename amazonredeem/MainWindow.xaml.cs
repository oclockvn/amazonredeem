/*
 * auto amazon redeem v1.0 build 1/5/2016
 * 
 * contact: oclockvn@gmail.com
 * 
 * todo: write log
 * 
 * */


using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace amazonredeem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // private WebRequestExtend _request = new WebRequestExtend();
        private int _totalSuccess = 0;
        private int _totalCode = 0;

        public MainWindow()
        {
            InitializeComponent();

            BrowseButton.Click += (sender, e) => OnBrowseFile();
            ProcessButton.Click += (sender, e) => OnProcess();
        }

        private void OnBrowseFile()
        {
            LogTextBlock.Text = "Auto Amazon Redeem v1.0. Created by oclockvn";

            var dialog = new OpenFileDialog()
            {
                DefaultExt = "*.txt",
                Filter = "*.txt|*.txt"
            };

            if (!dialog.ShowDialog().HasValue)
                return;

            var file = dialog.FileName;
            RedeemFileTextBox.Text = file; // enable button process
        }

        private void OnProcess()
        {
            var user = UserNameTextBox.Text;
            var pass = PasswordTextBox.Text;

            user = "xachtaydomy@gmail.com";
            pass = "ThanguyenKid@19";

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Dammitm, you forgot username or password again?");
                return;
            }

            var file = RedeemFileTextBox.Text;
            if (string.IsNullOrEmpty(file) || string.IsNullOrWhiteSpace(file))
            {
                MessageBox.Show("Redeem file not found");
                return;
            }

            ProcessButton.IsEnabled = false;

            // read code
            var codes = new List<string>();
            using (var reader = new StreamReader(file))
            {
                while (!reader.EndOfStream)
                {
                    var code = reader.ReadLine();
                    if (!string.IsNullOrEmpty(code) && !string.IsNullOrWhiteSpace(code))
                    {
                        codes.Add(code.Trim());
                    }
                }
            }

            if (codes.Count == 0)
            {
                MessageBox.Show("Code not found");
                return;
            }

            _totalCode = codes.Count;
            // each code post in separate task
            var tasks = new List<Task>();
            foreach (var code in codes)
            {
                var task = Task.Run(async () => await PostRedeemCode(code, user, pass));
                tasks.Add(task);
            }

            // notify when all task completed
            Task.WhenAll(tasks.ToArray())
                .ConfigureAwait(true)
                .GetAwaiter()
                .OnCompleted(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        LogTextBlock.Text = string.Format("{0}/{1} redeem success", _totalSuccess, _totalCode);
                        ProcessButton.IsEnabled = true;
                    });
                    MessageBox.Show("Auto Redeem Completed", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    _totalCode = 0;
                    _totalSuccess = 0;
                });
        }

        private async Task PostRedeemCode(string code, string user, string pass)
        {
            using (var request = new WebRequestExtend())
            {
                Debug.WriteLine("processing redeem code {0}", code, "");

                Dispatcher.Invoke(() => LogTextBlock.Text = "Login...");

                // download login form
                Debug.WriteLine("downloading login form");
                var loginFormString = await Task.Run(() =>
                {
                    return request.DownloadString("https://www.amazon.com/ap/signin?_encoding=UTF8&openid.assoc_handle=usflex&openid.claimed_id=http%3A%2F%2Fspecs.openid.net%2Fauth%2F2.0%2Fidentifier_select&openid.identity=http%3A%2F%2Fspecs.openid.net%2Fauth%2F2.0%2Fidentifier_select&openid.mode=checkid_setup&openid.ns=http%3A%2F%2Fspecs.openid.net%2Fauth%2F2.0&openid.ns.pape=http%3A%2F%2Fspecs.openid.net%2Fextensions%2Fpape%2F1.0&openid.pape.max_auth_age=0&openid.return_to=https%3A%2F%2Fwww.amazon.com%2F%3Fref_%3Dnav_signin");
                });

                // login
                //var loginFormString = await Task.Run(() =>
                //{
                //    return _request.DownloadString("https://www.amazon.com/ap/signin?_encoding=UTF8&openid.assoc_handle=usflex&openid.claimed_id=http%3A%2F%2Fspecs.openid.net%2Fauth%2F2.0%2Fidentifier_select&openid.identity=http%3A%2F%2Fspecs.openid.net%2Fauth%2F2.0%2Fidentifier_select&openid.mode=checkid_setup&openid.ns=http%3A%2F%2Fspecs.openid.net%2Fauth%2F2.0&openid.ns.pape=http%3A%2F%2Fspecs.openid.net%2Fextensions%2Fpape%2F1.0&openid.pape.max_auth_age=0&openid.return_to=https%3A%2F%2Fwww.amazon.com%2F%3Fref_%3Dnav_signin");
                //});

                if (string.IsNullOrEmpty(loginFormString))
                {
                    Debug.WriteLine("login form not found");
                    return;
                }

                Debug.WriteLine("login form download completed: {0}", loginFormString.Substring(0, loginFormString.Length / 100) + "...");

                // create form to post data
                Debug.WriteLine("create login form with user={0}, pass={1}", user, pass);
                var loginForm = Tools.GetInputs(loginFormString, "signIn") ?? new Dictionary<string, string>();
                if (loginForm.Count == 0)
                {
                    loginForm.Add("email", string.Empty);
                    loginForm.Add("password", string.Empty);
                }

                loginForm["email"] = user;
                loginForm["password"] = pass;

                var postLogin = Tools.GetUrlEncoded(loginForm);
                Debug.WriteLine("posting login form: {0}", postLogin, "");

                var accessTokenForm = await Task.Run(() =>
                {
                    try
                    {
                        request.UploadString("https://www.amazon.com/ap/signin", postLogin);
                        Debug.WriteLine("login success");
                        return request.DownloadString("https://www.amazon.com/gc/redeem"); // download cookies
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("login error: {0}", ex.Message, "");
                        return string.Empty;
                    }
                });

                Debug.WriteLine("posting redeem code {0}", code, "");
                Dispatcher.Invoke(() => LogTextBlock.Text = string.Format("Login success. Processing redeem code {0}", code));

                // get acess token
                // var accessTokenForm = await Task.Run(() => _request.DownloadString("https://www.amazon.com/gc/redeem"));

                // get cross-sharing-resource-token
                var pattern = @"<input type='hidden' name='csrf' value='(.*)' />";
                var csrf = string.Empty;
                var m = Regex.Match(accessTokenForm, pattern);
                if (m.Success)
                {
                    csrf = m.Groups[1].Value;
                }

                // create redeem form
                var redeem = new Dictionary<string, string>();
                redeem["csrf"] = csrf;
                redeem["claimCode"] = code;
                var redeemData = Tools.GetUrlEncoded(redeem);
                Debug.WriteLine("redeem params: {0}", redeemData, "");

                var result = await Task.Run(() => request.UploadString("https://www.amazon.com/gc/redeem/result", redeemData)) ?? string.Empty;
                var success = result.Contains("$1.00 has been added to your Gift Card Balance");

                Debug.WriteLine("redeem result: {0}", result.Substring(0, result.Length / 100) + "...", "");

                if (success)
                {
                    _totalSuccess++;
                }

                Debug.WriteLine("{0}/{1} redeem successful", _totalSuccess, _totalCode);
                Dispatcher.Invoke(() => LogTextBlock.Text = string.Format("{0}/{1} redeem successful", _totalSuccess, _totalCode));
            }
        }
    }
}
