using System.Collections.Generic;
using Oxide.Core.Configuration;
using UnityEngine;
using Oxide.Core;
using System;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Physics = UnityEngine.Physics;

namespace Oxide.Plugins
{
    [Info("Flying Boat", "JazzDevs", "0.1")]
    [Description("Just Messing Around And Getting To Know Things About Rust Again So I Made This Plugin Hope You Guys Enjoy!, Might Not Work As Intended Dont Have A Way To Test It!")]
    class Flyingboat : RustPlugin
    {
        public static Flyingboat plugin;

        private const string permAllow = "flyingboat.allow";
        private bool logToConsole = true;
        private bool logAdminInfo = true;
        private string commandToRun = "flyingboat";
        private string chatCommand = "fb";
        private bool isActive = false;
        public Rigidbody Rigidbody;
        public BaseVehicle Vehicle;
        public bool IsEnabled = false;

        private readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("Flyingboat");

        private List<ulong> Users = new List<ulong>();
        protected override void LoadDefaultConfig()
        {
            Config["LogToConsole"] = logToConsole = GetConfig("LogToFile", true);
            Config["LogAdminInfo"] = logAdminInfo = GetConfig("LogAdminInfo", true);
            Config["CommandToRun (Leave blank for no command)"] = commandToRun = GetConfig("CommandToRun (Leave blank for no command)", "box");
            Config["ChatCommand"] = chatCommand = GetConfig("ChatCommand", "b");

            SaveConfig();
        }

        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoEntityFound"] = "No entity found.",
                ["BoatActivated"] = "You have enabled FlyingBoat.",
                ["BoatDeactivated"] = "You have disabled FlyingBoat.",
            }, this);
        }

        private void Init() {

            plugin = this;

            Users = dataFile.ReadObject<List<ulong>>();

            LoadDefaultConfig();
            permission.RegisterPermission(permAllow, this);

            cmd.AddChatCommand("fb", this, "CmdFlyingBoat");
            cmd.AddChatCommand("flyingboat", this, "CmdFlyingBoat");

        }

        private void CmdFlyingBoat(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, permAllow)) return;

            if (Users.Contains(player.userID))
            {
                Users.Remove(player.userID);

                player.ChatMessage(Lang("BoatDeactivated", player.UserIDString));

                isActive = false;
            }
            else
            {
                Users.Add(player.userID);


                player.ChatMessage(Lang("BoatActivated", player.UserIDString));

                isActive = true;
            }


        }

        private void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {
            if (entity == null)
                return;

            if (isActive == false)
            {
                Rigidbody.useGravity = true;
            }
            else 
            {
                Rigidbody.useGravity = false;
                Rigidbody.AddForce(Vector3.up * 50f, ForceMode.Impulse);
            }
        }

        private void FixedUpdate()
        {
            if ((Vehicle?.IsDestroyed ?? true) || Rigidbody == null)
            {
                DestroyImmediate(this);
                return;
            }

            Rigidbody.drag = 0.6f;
            Rigidbody.angularDrag = 5.0f;

            if (!IsEnabled)
                return;

            if (!isActive)
                return;

            var driver = Vehicle.GetDriver();
            if (driver == null)
                return;

            var input = driver.serverInput;
            if (input.IsDown(BUTTON.RELOAD))
            {
                Rigidbody.drag *= 5;
                Rigidbody.angularDrag *= 2;
            }

            var direction = Vector3.zero;

            if (input.IsDown(BUTTON.FORWARD))
                direction += Vector3.forward;

            if (input.IsDown(BUTTON.BACKWARD))
                direction += Vector3.back;

            if (direction != Vector3.zero)
            {
                const float moveSpeed = 500f;
                var speed = input.IsDown(BUTTON.SPRINT) ? moveSpeed * 3f : moveSpeed;

                Rigidbody.AddRelativeForce(direction * speed, ForceMode.Force);
            }

            var torque = Vector3.zero;

            const float rotationSpeed = 700f;
            if (input.IsDown(BUTTON.LEFT))
            {
                torque += new Vector3(0, -rotationSpeed, 0);
            }

            if (input.IsDown(BUTTON.RIGHT))
            {
                torque += new Vector3(0, rotationSpeed, 0);
            }

            const float mouseSpeedY = 400f;
            const float mouseSpeedX = 100f;

            var mouse = input.current.mouseDelta;
            torque += new Vector3(mouse.y * mouseSpeedY, 0, mouse.x * -mouseSpeedX);

            Rigidbody.AddRelativeTorque(torque, ForceMode.Force);
        }

        private void Awake()
        {
            if (!isActive) //Checks If Their In A Vehicle
                return;

            Rigidbody = GetComponent<Rigidbody>();
            Vehicle = GetComponent<BaseVehicle>();

            Rigidbody.velocity = Vector3.down;
            Rigidbody.mass = 50f;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.centerOfMass = new Vector3(0.0f, -0.2f, 1.4f);
            Rigidbody.inertiaTensor = new Vector3(220.8f, 207.3f, 55.5f);
        }
    }


}
