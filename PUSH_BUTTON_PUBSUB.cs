using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using UnityEngine.UI;
using System.Threading;



public class PUSH_BUTTON_PUBSUB : MonoBehaviour
{
    public static string host = "localhost"; // Solace messaging router host name or IP address
    public static string username = "admin";
    public static string vpnname = "default";
    public static string password = "admin";
    bool bootdone = false;
   
    public static string ButtonStatusSend = "0";
    public static string ButtonStatusSendGreenScript = "0";

    public static string ButtonStatusSendred = "0";
    public static string msgReceive = "0-0";
   
    public GameObject led;
   
    TextMesh gaugelabel;

    // Initialize Solace Systems Messaging API with logging to console at Warning level
    ContextFactoryProperties cfp = new ContextFactoryProperties()
    {
        SolClientLogLevel = SolLogLevel.Warning
    };

    //the percentage of button pressing to activate button press is threshold
    [SerializeFeild] public float threshold = 0.1f;
    // if there is any boundiness in the end, prevent to press and release the button whole bunch of time
    [SerializeFeild] public float deadzone = 0.025f;

    //Track the button is pressed or not is our state management
    bool _isPressed;
    //start position is going to help us to compare start position and current position to tell us how far the button is moved
    public Vector3 _stratPos;
    // public Vector3 _endPos;

    private ConfigurableJoint _joint;

    public UnityEngine.Events.UnityEvent onPressed, onReleased;

 


    void Start() 
    {              
         _stratPos= transform.localPosition;
         _joint = GetComponent<ConfigurableJoint>();
         led.SetActive(false);
         gaugelabel = GameObject.Find("Gauge_label").GetComponent<TextMesh>();
        
    }

    #region SubscribingThread
    static void SubscribingThread()
    {
        // Initialize Solace Systems Messaging API with logging to console at Warning level
        ContextFactoryProperties cfp = new ContextFactoryProperties()
        {
            SolClientLogLevel = SolLogLevel.Warning
        };
        cfp.LogToConsoleError();

        while (true)
        {
            try
            {
                // Context must be created first
                ContextFactory.Instance.Init(cfp);
                using (IContext context = ContextFactory.Instance.CreateContext(new ContextProperties(), null))
                {
                    // Create the application
                    TopicSubscriber topicSubscriber = new TopicSubscriber();
                    topicSubscriber.VPNName = vpnname;
                    topicSubscriber.UserName = username;
                    topicSubscriber.Password = password;
                    topicSubscriber.Run(context, host);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown: {0}", ex.Message);
            }
            finally
            {
                // Dispose Solace Systems Messaging API
                ContextFactory.Instance.Cleanup();
            }
            Debug.Log(" SUBSCRIBING.");
        }
    }
    #endregion

    public void Update()
    {

        // get this msgReceive from Arduino
        string[] splitArray = msgReceive.Split('-');
        
        Dictionary<string, string> Msg_Rec = new Dictionary<string, string>();
        Debug.Log(splitArray.Length);
        Msg_Rec.Add("ButtonStatusred", splitArray[0]);
        Msg_Rec.Add("LedStatus", splitArray[1]);
        
        if (splitArray.Length > 2)
            gaugelabel.text = splitArray[2];

        if (splitArray.Length > 3)
            Msg_Rec.Add("ButtonStatusGreen", splitArray[3]);
    
        ButtonStatusSendGreenScript = GREEN.ButtonStatusSendGreen;

       
        if (!_isPressed && GetValue() + threshold >= 1)
        {
            Pressed();
           
        }
       
        if (_isPressed && GetValue() - threshold <= 0)
        {
            Released();
           
        }
          
        if (bootdone == false)
        {
            cfp.LogToConsoleError(); bootdone = true;
            //start subscriber
            Thread t = new Thread(SubscribingThread);
            t.Start();
        }
           
        if (Msg_Rec["ButtonStatusred"] == "1")
        {
           
            gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y - 0.002f, gameObject.transform.position.z);
           
        }

        if (Msg_Rec["ButtonStatusGreen"] == "1")
        {
            Released();
         
        }

        if (Msg_Rec["LedStatus"] == "1")
        {
            led.SetActive(true);
           
        }
        if (Msg_Rec["LedStatus"] == "0")
        {
            led.SetActive(false);
         
        }


        try
        {
            // Context must be created first
            ContextFactory.Instance.Init(cfp);
            using (IContext context = ContextFactory.Instance.CreateContext(new ContextProperties(), null))
            {
                // Create the application
                TopicPublisher topicPublisher = new TopicPublisher()
                {
                    VPNName = vpnname,
                    UserName = username,
                    Password = password
                };

                // Run the application within the context and against the host
                topicPublisher.Run(context, host);
            

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception thrown: {0}", ex.Message);
        }
        finally
        {
            // Dispose Solace Systems Messaging API
            ContextFactory.Instance.Cleanup();
        }
        Debug.Log(" PUBLISHING");
    }

    #region Subscriber
    class TopicSubscriber
    {
        public string VPNName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        const int DefaultReconnectRetries = 3;

        private ISession Session = null;
        private EventWaitHandle WaitEventWaitHandle = new AutoResetEvent(false);

        public void Run(IContext context, string host)
        {
            // Validate parameters
            if (context == null)
            {
                throw new ArgumentException("Solace Systems API context Router must be not null.", "context");
            }
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Solace Messaging Router host name must be non-empty.", "host");
            }
            if (string.IsNullOrWhiteSpace(VPNName))
            {
                throw new InvalidOperationException("VPN name must be non-empty.");
            }
            if (string.IsNullOrWhiteSpace(UserName))
            {
                throw new InvalidOperationException("Client username must be non-empty.");
            }

            // Create session properties
            SessionProperties sessionProps = new SessionProperties()
            {
                Host = host,
                VPNName = VPNName,
                UserName = UserName,
                Password = Password,
                ReconnectRetries = DefaultReconnectRetries
            };

            // Connect to the Solace messaging router
            Console.WriteLine("Connecting as {0}@{1} on {2}...", UserName, VPNName, host);
            // NOTICE HandleMessage as the message event handler
            Session = context.CreateSession(sessionProps, HandleMessage, null);
            ReturnCode returnCode = Session.Connect();
            if (returnCode == ReturnCode.SOLCLIENT_OK)
            {
                Console.WriteLine("Session successfully connected.");

                // This is the topic on Solace messaging router where a message is published
                // Must subscribe to it to receive messages
                Session.Subscribe(ContextFactory.Instance.CreateTopic("try/subUnity"), true);

                Console.WriteLine("Waiting for a message to be published...");
                WaitEventWaitHandle.WaitOne();
            }
            else
            {
                Console.WriteLine("Error connecting, return code: {0}", returnCode);
            }
        }

        /// <summary>
        /// This event handler is invoked by Solace Systems Messaging API when a message arrives
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        public void HandleMessage(object source, MessageEventArgs args)
        {
            Console.WriteLine("Received published message.");
            // Received a message
            using (IMessage message = args.Message)
            {
                // Expecting the message content as a binary attachment
                Console.WriteLine("Message content: {0}", Encoding.ASCII.GetString(message.BinaryAttachment));
                string NewMessage = Encoding.ASCII.GetString(message.BinaryAttachment);
                msgReceive = NewMessage;
              
                if (NewMessage == "1") 
                {
                    Debug.Log(" Push button pressed in real factory ");

                }
                // finish the program
                WaitEventWaitHandle.Set();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        public virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Session != null)
                    {
                        Session.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
    #endregion

    #region Publisher
    class TopicPublisher
    {
        public string VPNName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        const int DefaultReconnectRetries = 3;

        private ISession Session = null;
        //private ISession Session1 = null;
        private EventWaitHandle WaitEventWaitHandle = new AutoResetEvent(false);

        public void Run(IContext context, string host)
        {
            // Validate parameters
            if (context == null)
            {
                throw new ArgumentException("Solace Systems API context Router must be not null.", "context");
            }
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Solace Messaging Router host name must be non-empty.", "host");
            }
            if (string.IsNullOrWhiteSpace(VPNName))
            {
                throw new InvalidOperationException("VPN name must be non-empty.");
            }
            if (string.IsNullOrWhiteSpace(UserName))
            {
                throw new InvalidOperationException("Client username must be non-empty.");
            }

            // Create session properties
            SessionProperties sessionProps = new SessionProperties()
            {
                Host = host,
                VPNName = VPNName,
                UserName = UserName,
                Password = Password,
                ReconnectRetries = DefaultReconnectRetries
            };

            // Connect to the Solace messaging router
            Console.WriteLine("Connecting as {0}@{1} on {2}...", UserName, VPNName, host);
            using (ISession session = context.CreateSession(sessionProps, null, null))
            {
                ReturnCode returnCode = session.Connect();
                if (returnCode == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected.");
                    PublishMessage(session);
                }
                else
                {
                    Console.WriteLine("Error connecting, return code: {0}", returnCode);
                    Console.ReadLine();
                }
            }
        }

        private void PublishMessage(ISession session)
        {
            // Create the message
            using (IMessage messagepub = ContextFactory.Instance.CreateMessage())
            {
                messagepub.Destination = ContextFactory.Instance.CreateTopic("try/pubUnity");
               

                 ButtonStatusSend = ButtonStatusSendred + "-"+ ButtonStatusSendGreenScript;
            
                 messagepub.BinaryAttachment = Encoding.ASCII.GetBytes(ButtonStatusSend);
               
                Console.WriteLine("Publishing message...");
                
                ReturnCode returnCode = session.Send(messagepub);
                
                
                
                if (returnCode == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Done.");
                }
                else
                {
                    Console.WriteLine("Publishing failed, return code: {0}", returnCode);
                    Console.ReadLine();
                }
            }
        }
    }
    #endregion
    #region
    public float GetValue()
    {
        
        var value = Vector3.Distance(_stratPos, transform.localPosition) / _joint.linearLimit.limit;

        if (System.Math.Abs(value) < deadzone)
            value = 0;
        return Mathf.Clamp(value, min: -1f, max: 1f);
    }


    public void Pressed()
    {
        _isPressed = true;
        onPressed.Invoke();
    
    }

    public void Released()
    {
        _isPressed = false;
        onReleased.Invoke();
        ButtonStatusSendred = "0";
       

    }
    #endregion

   
}



       

         
    





