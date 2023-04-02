//#############################################################################
//#
//#   Return to Blockland - Version 4
//#
//#   -------------------------------------------------------------------------
//#
//#      $Rev: 533 $
//#      $Date: 2011-12-06 19:04:19 +0000 (Tue, 06 Dec 2011) $
//#      $Author: Ephialtes $
//#      $URL: http://svn.returntoblockland.com/code/trunk/modules/client/connectClient.cs $
//#
//#      $Id: connectClient.cs 533 2011-12-06 19:04:19Z Ephialtes $
//#
//#      Copyright (c) 2008 - 2010 by Nick "Ephialtes" Matthews
//#
//#   -------------------------------------------------------------------------
//#
//#   Modules / Client / Connect Client
//#
//#############################################################################
//Register that this module has been loaded
$RTB::Modules::Client::ConnectClient = 1;

//*********************************************************
//* Variable Declarations
//*********************************************************
$RTB::MCCC::Server = "connect.returntoblockland.com";
$RTB::MCCC::Port = 14000;

//*********************************************************
//* Sound Profiles
//*********************************************************
if(!isObject(RTBCC_TickSound))
   new AudioProfile(RTBCC_TickSound)
   {
      fileName = $RTB::Path@"sounds/tick.wav";
      description = "AudioGui";
      preload = "1";
   };
   
if(!isObject(RTBCC_OnlineSound))
   new AudioProfile(RTBCC_OnlineSound)
   {
      fileName = $RTB::Path@"sounds/boop1.wav";
      description = "AudioGui";
      preload = "1";
   };
   
if(!isObject(RTBCC_MessageSound))
   new AudioProfile(RTBCC_MessageSound)
   {
      fileName = $RTB::Path@"sounds/beep1.wav";
      description = "AudioGui";
      preload = "1";
   };
   
if(!isObject(RTBCC_JoinSound))
   new AudioProfile(RTBCC_JoinSound)
   {
      fileName = $RTB::Path@"sounds/beep2.wav";
      description = "AudioGui";
      preload = "1";
   };

//*********************************************************
//* Connect Client GUI Interaction
//********************************************************
//- RTB_ConnectClient::onWake (gui on wake callback)
function RTB_ConnectClient::onWake(%this)
{
   %this.resetCursor();

   %this.prepare();
   %this.setAvatar();
}

//- RTB_ConnectClient::escape (call for people who close the client)
function RTB_ConnectClient::escape(%this,%direct)
{
   if(!%direct)
   {
      if(RTBCC_Modal.isVisible())
      {
         %this.closeModalWindow();
         return;
      }
   }
   RTB_Overlay.pop(%this);
}

//- RTB_ConnectClient::init (prepares connect client for usage)
function RTB_ConnectClient::init(%this)
{
   if(!RTBCC_Avatar.hasBeenSet)
   {
      RTBCC_Avatar.setObject("","base/data/shapes/player/m.dts","",100);
      RTBCC_Avatar.hasBeenSet = 1;
   }
   RTB_ConnectClient.setDetails();
      
   if(isObject(RTBCC_Socket))
   {
      RTBCC_Socket.disconnect();
      RTBCC_Socket.parser.delete();
      RTBCC_Socket.delete();
   }
   RTBCC_createInputRecycler();
   RTBCC_createRoomOptionsManager();
   RTBCC_createNotificationManager();
   
   %socket = new TCPObject(RTBCC_Socket)
   {
      connected = 0;
      authenticated = 0;
      
      trying = 0;
      tries = 0;
      
      host = $RTB::MCCC::Server;
      port = $RTB::MCCC::Port;
      
      token = 0;
      
      roster = RTBCC_createRoster();
      tempRoster = RTBCC_createTempRoster();
      roomManager = RTBCC_createRoomManager();
      inviteRoster = RTBCC_createInviteRoster();
      sessionManager = RTBCC_createSessionManager();
      roomSessionManager = RTBCC_createRoomSessionManager();
      
      version = 2;
   };
   RTBGroup.add(%socket);
   
   %parser = new ScriptGroup(RTBCC_XMLParser)
   {
      class = "XMLParser";
   };
   %socket.parser = %parser;
   RTBGroup.add(%parser);
   
   %parser.registerHandler("error","RTBCC_Socket::onErrorPacket",%socket);
   %parser.registerHandler("success","RTBCC_Socket::onErrorPacket",%socket);
   %parser.registerHandler("response","RTBCC_Socket::onErrorPacket",%socket);
   
   %parser.registerHandler("notice","RTBCC_Socket::onNoticePacket",%socket);
   %parser.registerHandler("auth","RTBCC_Socket::onAuthPacket",%socket);
   %parser.registerHandler("roster","RTBCC_Socket::onRosterPacket",%socket);
   %parser.registerHandler("presence","RTBCC_Socket::onPresencePacket",%socket);
   %parser.registerHandler("message","RTBCC_Socket::onMessagePacket",%socket);
   %parser.registerHandler("action","RTBCC_Socket::onActionPacket",%socket);
   
   %parser.registerHandler("ping","RTBCC_Socket::onPingPacket",%socket);
   %parser.registerHandler("disconnect","RTBCC_Socket::onDisconnectPacket",%socket);
}

//- RTB_ConnectClient::setDetails (updates the details in the gui)
function RTB_ConnectClient::setDetails(%this)
{
   %this.client_id = getNumKeyID();
   %this.client_name = $pref::Player::NetName;
   
   RTBCC_NetName.setText("<color:666666><font:Impact:18>"@%this.client_name);
   RTBCC_BLID.setText("<color:888888><font:Arial:12>Blockland ID "@%this.client_id);
}

//- RTB_ConnectClient::changeStatus (opens status changing modal)
function RTB_ConnectClient::changeStatus(%this)
{
   if(RTBCC_Socket.connected && RTBCC_Socket.authenticated)
      RTB_ConnectClient.setModalWindow("ChangeStatus");
}

//- RTB_ConnectClient::setStatus (sets the user's status)
function RTB_ConnectClient::setStatus(%this,%status,%update)
{
   if(%status $= "online")
   {
      RTBCC_StatusIcon.setBitmap($RTB::Path@"images/icons/status_online");
      RTBCC_StatusText.setText("<font:Verdana:12><color:777777>Online<bitmap:"@$RTB::Path@"images/icons/bullet_arrow_down>");
   }
   else if(%status $= "away")
   {
      RTBCC_StatusIcon.setBitmap($RTB::Path@"images/icons/status_away");
      RTBCC_StatusText.setText("<font:Verdana:12><color:777777>Away<bitmap:"@$RTB::Path@"images/icons/bullet_arrow_down>");
   }
   else if(%status $= "busy")
   {
      RTBCC_StatusIcon.setBitmap($RTB::Path@"images/icons/status_busy");
      RTBCC_StatusText.setText("<font:Verdana:12><color:777777>Busy<bitmap:"@$RTB::Path@"images/icons/bullet_arrow_down>");
   }
   else if(%status $= "hidden")
   {
      RTBCC_StatusIcon.setBitmap($RTB::Path@"images/icons/status_offline");
      RTBCC_StatusText.setText("<font:Verdana:12><color:777777>Hidden<bitmap:"@$RTB::Path@"images/icons/bullet_arrow_down>");
   }
   else
   {
      RTBCC_StatusIcon.setBitmap($RTB::Path@"images/icons/status_offline");
      RTBCC_StatusText.setText("<font:Verdana:12><color:777777>Offline");
   }
   
   if(%update)
   {
      RTBCC_Socket.sendStatus(%status);
      
      RTB_ConnectClient.closeModalWindow();
   }
}

//- RTB_ConnectClient::setAvatar (sets the user's avatar)
function RTB_ConnectClient::setAvatar(%this)
{
   if($hat0 $= "")
   {
      AvatarGui.onWake();
      
      //@HACK
      // - If the mdts tssconstructor exists as datablocks are loaded, explosions occur
      deleteDatablocks();
   }
      
   RTBCC_Avatar.forceFOV = 18;
   RTBCC_Avatar.setOrbitDist(6);
   RTBCC_Avatar.setCameraRot(0.22,0.5,2.8);
   RTBCC_Avatar.lightDirection = "0 0.2 0.2";   
      
   %face = $Pref::Avatar::FaceName;
   %skincolor = $Pref::Avatar::HeadColor;
   %hat = $Pref::Avatar::Hat;
   %hatColor = $Pref::Avatar::HatColor;
   %accent = $Pref::Avatar::Accent;
   %accentColor = $Pref::Avatar::AccentColor;
   %chest = $Pref::Avatar::Chest;
   %chestColor = $Pref::Avatar::TorsoColor;
   %arm = $Pref::Avatar::LArm;
   %armColor = $pref::Avatar::LArmColor;
   %chestDecal = $Pref::Avatar::DecalName;
   %pack = $pref::Avatar::Pack;
   %packColor = $pref::Avatar::PackColor;
   %secondPack = $Pref::Avatar::SecondPack;
   %secondPackColor = $pref::Avatar::SecondPackColor;
   
   %i=0;
   while($face[%i] !$= "")
   {
      if($face[%i] $= %face)
      {
         %face = %i;
         break;
      }
      %i++;
      
      if(%i > 500)
         break;
   }
   
   %i=0;
   while($decal[%i] !$= "")
   {
      if($decal[%i] $= %chestDecal)
      {
         %chestDecal = %i;
         break;
      }
      %i++;
      
      if(%i > 500)
         break;
   }

   %parts = "accent hat chest pack secondpack larm rarm lhand rhand hip lleg rleg";
   for(%i=0;%i<getWordCount(%parts);%i++)
   {
      %k = 0;
      eval("%partName = $"@getWord(%parts,%i)@%k@";");
      while(%partName !$= "")
      {
         if(%partName !$= "none")
            RTBCC_Avatar.hidenode("",%partName);
         eval("%partName = $"@getWord(%parts,%i)@%k++@";");
         %b++;
      }
   }
   
   RTBCC_Avatar.setNodeColor("","ALL",%skinColor);
   RTBCC_Avatar.setIFLFrame("","face",%face);
   RTBCC_Avatar.setIFLFrame("","decal",%chestDecal);
   
   if(%hat !$= "" && %hat !$= 0)
   {
      RTBCC_Avatar.unhidenode("",$hat[%hat]);
      RTBCC_Avatar.setnodeColor("",$hat[%hat],%hatColor);
      
      %accent = getWord($accentsAllowed[$hat[%hat]],%accent);
      if(%accent !$= "" && %accent !$= "none")
      {
         RTBCC_Avatar.unhidenode("",%accent);
         RTBCC_Avatar.setnodeColor("",%accent,%accentColor);
      }
   }
   
   if(%pack !$= "" && %pack !$= 0)
   {
      RTBCC_Avatar.unhidenode("",$pack[%pack]);
      RTBCC_Avatar.setnodeColor("",$pack[%pack],%packColor);
   }
   if(%secondpack !$= "" && %secondpack !$= 0)
   {
      RTBCC_Avatar.unhidenode("",$secondpack[%secondpack]);
      RTBCC_Avatar.setnodeColor("",$secondpack[%secondpack],%secondpackColor);
   }
   RTBCC_Avatar.unhidenode("",$chest[%chest]);
   RTBCC_Avatar.setNodeColor("",$chest[%chest],%chestColor);
   RTBCC_Avatar.unhidenode("",$larm[%arm]);
   RTBCC_Avatar.setNodeColor("",$larm[%arm],%armColor);
   RTBCC_Avatar.unhidenode("",$rarm[%arm]);
   RTBCC_Avatar.setNodeColor("",$rarm[%arm],%armColor);
   
   RTBCC_Avatar.setMouse(0,0);
}

//- RTB_ConnectClient::enableInterface (enables buttons on the roster interface)
function RTB_ConnectClient::enableInterface(%this)
{
   RTBCC_Tab_Roster.setActive(true);
   RTBCC_Tab_Chat.setActive(true);
   RTBCC_Tab_Options.setActive(true);
   
   RTBCC_Connect.setVisible(false);
   RTBCC_Disconnect.setVisible(true);
}

//- RTB_ConnectClient::disableInterface (disables buttons on the roster interface)
function RTB_ConnectClient::disableInterface(%this)
{
   RTBCC_Tab_Roster.setActive(false);
   RTBCC_Tab_Chat.setActive(false);
   RTBCC_Tab_Options.setActive(false);
   
   RTBCC_Connect.setVisible(true);
   RTBCC_Disconnect.setVisible(false);
   
   %this.setPane(RTBCC_Window_Splash);
}

//- RTB_ConnectClient::prepare (prepares the gui for usage)
function RTB_ConnectClient::prepare(%this)
{
   if(!RTBCC_Socket.connected)
   {
      %this.disableInterface();
      if(RTBCC_Socket.trying)
      {
         RTBCC_Disconnect.setVisible(true);
         RTBCC_Connect.setVisible(false);
      }
   }
   else
      %this.enableInterface();
}

//- RTB_ConnectClient::resetCursor (finds the best place to put the cursor back to)
function RTB_ConnectClient::resetCursor(%this)
{
   %lastActive = RTB_Overlay.getObject(RTB_Overlay.getCount()-1);
   if(%lastActive.session)
      %lastActive.session.focus();
}

//- RTB_ConnectClient::onSleep (gui on sleep callback)
function RTB_ConnectClient::onSleep(%this)
{
}

//- RTB_ConnectClient::setPane (sets the pane for the gui)
function RTB_ConnectClient::setPane(%this,%pane)
{
   if(%this.currPane $= %pane && %this.currPane.isVisible())
      return;
      
   RTBCC_Window_Splash.setVisible(false);
   RTBCC_Window_Roster.setVisible(false);
   RTBCC_Window_Chat.setVisible(false);
   RTBCC_Window_Options.setVisible(false);
   
   if(isObject(%this.currPane) && %this.currPane.getID() !$= %pane.getID())
      eval(%this.currPane.getName()@"::onSleep("@%this.currPane@");");
   
   if(isObject(%pane))
   {
      %this.currPane = %pane;
      %pane.setVisible(true);
      eval(%pane@"::onWake("@%pane@");");
   }
}

//*********************************************************
//* Client Tab Callbacks and Management
//********************************************************
//- RTBCC_Window_Roster::onWake (on wake callback)
function RTBCC_Window_Roster::onWake(%this)
{
}

//- RTBCC_Window_Roster::onSleep (on sleep callback)
function RTBCC_Window_Roster::onSleep(%this)
{
}

//- RTBCC_Window_Chat::onWake (on wake callback)
function RTBCC_Window_Chat::onWake(%this)
{
   if(!%this.isVisible())
      return;
      
   if(RTBCC_Socket.connected)
      RTBCC_RoomManager.refresh();
}

//- RTBCC_Window_Chat::onSleep (on sleep callback)
function RTBCC_Window_Chat::onSleep(%this)
{
   RTBCC_RoomManager.stopRefresh();
}

//- RTBCC_Window_Options::registerGroup (registers a group for the options menu)
function RTBCC_Window_Options::registerGroup(%this,%name,%icon)
{
   if(%this.group[%name])
      return;
      
   %this.group[%this.groups] = %name TAB %icon;
   %this.group[%name] = 1;
   %this.groups++;
}

//- RTBCC_Window_Options::registerPref (registers a pref for the options menu)
function RTBCC_Window_Options::registerPref(%this,%name,%group,%pref,%type)
{
   if(%this.pref[%pref])
      return;
      
   %this.pref[%this.prefs] = %name TAB %type;
   %this.pref[%pref] = 1;
   %this.prefToStore[%this.prefs] = %pref;
   %this.prefToGroup[%this.prefs] = %group;
   %this.prefs++;
}

//- RTBCC_Window_Options::onWake (on wake callback, loads cc prefs)
function RTBCC_Window_Options::onWake(%this)
{
   if(!%this.isVisible())
      return;
      
   %pointer = "0 0";
   RTBCC_Options_Swatch.clear();
   for(%i=0;%i<%this.groups;%i++)
   {
      %groupName = getField(%this.group[%i],0);
      %icon = getField(%this.group[%i],1);
      
      %icon = new GuiBitmapCtrl()
      {
         position = vectorAdd(%pointer,"6 2");
         extent = "16 16";
         bitmap = $RTB::Path@"images/icons/"@%icon;
      };
      RTBCC_Options_Swatch.add(%icon);
      
      %text = new GuiMLTextCtrl()
      {
         position = vectorAdd(%pointer,"26 4");
         extent = "150 12";
         text = "<font:Verdana Bold:12><color:444444>"@%groupName;
         selectable = false;
      };
      RTBCC_Options_Swatch.add(%text);
      
      %divider = new GuiBitmapCtrl()
      {
         position = vectorAdd(%pointer,"4 20");
         extent = "175 3";
         bitmap = $RTB::Path@"images/ui/dottedLine";
         wrap = true;
      };
      RTBCC_Options_Swatch.add(%divider);
      
      %pointer = vectorAdd(%pointer,"0 27");
      
      for(%j=0;%j<%this.prefs;%j++)
      {
         if(%this.prefToGroup[%j] !$= %groupName)
            continue;
            
         %name = getField(%this.pref[%j],0);
         %type = getField(%this.pref[%j],1);
         
         if(%type $= "bool")
         {
            %text = new GuiMLTextCtrl()
            {
               position = vectorAdd(%pointer,"7 1");
               extent = "155 12";
               text = "<color:888888><font:Verdana:12>"@%name;
               selectable = false;
            };
            RTBCC_Options_Swatch.add(%text);
            
            %box = new GuiCheckboxCtrl()
            {
               profile = "RTB_CheckboxProfile";
               position = vectorAdd(%pointer,"163 0");
               extent = "16 16";
               text = " ";
               pref = %j;
               prefName = %this.prefToStore[%j];
               prefValue = 1;
               command = %this@".done();";
            };
            RTBCC_Options_Swatch.add(%box);
            
            if(RTBCO_getPref(%this.prefToStore[%j]) $= 1)
               %box.setValue(1);
            
            %pointer = vectorAdd(%pointer,"0 17");
         }
         else
         {
            %parameters = getSubStr(%type,strPos(%type,"{")+1,strPos(%type,"}")-strPos(%type,"{")-1);
            %type = getSubStr(%type,0,strPos(%type,":"));
            if(%type $= "list")
            {
               %text = new GuiMLTextCtrl()
               {
                  position = vectorAdd(%pointer,"7 1");
                  extent = "180 12";
                  text = "<color:888888><font:Verdana:12>"@%name;
                  selectable = false;
               };
               RTBCC_Options_Swatch.add(%text);
               
               %pointer = vectorAdd(%pointer,"0 14");
               
               %parameters = strReplace(%parameters,",","\t");
               for(%k=0;%k<getFieldCount(%parameters);%k++)
               {
                  %param = getField(%parameters,%k);
                  %name = getSubStr(%param,0,strPos(%param,":"));
                  %value = getSubStr(%param,strPos(%param,":")+1,strLen(%param));
                  
                  %text = new GuiMLTextCtrl()
                  {
                     position = vectorAdd(%pointer,"11 1");
                     extent = "145 12";
                     text = "<just:right><color:AAAAAA><font:Verdana:12>"@%name;
                     selectable = false;
                  };
                  RTBCC_Options_Swatch.add(%text);
                  
                  %box = new GuiRadioCtrl()
                  {
                     profile = "RTB_RadioButtonProfile";
                     position = vectorAdd(%pointer,"163 0");
                     extent = "16 16";
                     text = " ";
                     pref = %j;
                     groupNum = %j;
                     prefName = %this.prefToStore[%j];
                     prefValue = %value;
                     command = %this@".schedule(1,\"done\");";
                  };
                  RTBCC_Options_Swatch.add(%box);
                  
                  if(RTBCO_getPref(%this.prefToStore[%j]) $= %value)
                     %box.performClick();
                  
                  %pointer = vectorAdd(%pointer,"0 16");
               }
            }
            else if(%type $= "multibool")
            {
               %text = new GuiMLTextCtrl()
               {
                  position = vectorAdd(%pointer,"7 1");
                  extent = "180 12";
                  text = "<color:888888><font:Verdana:12>"@%name;
                  selectable = false;
               };
               RTBCC_Options_Swatch.add(%text);
               
               %pointer = vectorAdd(%pointer,"0 14");
               
               %parameters = strReplace(%parameters,",","\t");
               for(%k=0;%k<getFieldCount(%parameters);%k++)
               {
                  %param = getField(%parameters,%k);
                  %name = getSubStr(%param,0,strPos(%param,":"));
                  %prefAppend = getSubStr(%param,strPos(%param,":")+1,strLen(%param));
                  
                  %text = new GuiMLTextCtrl()
                  {
                     position = vectorAdd(%pointer,"11 1");
                     extent = "145 12";
                     text = "<just:right><color:AAAAAA><font:Verdana:12>"@%name;
                     selectable = false;
                  };
                  RTBCC_Options_Swatch.add(%text);
                  
                  %box = new GuiCheckboxCtrl()
                  {
                     profile = "RTB_CheckboxProfile";
                     position = vectorAdd(%pointer,"163 0");
                     extent = "16 16";
                     text = " ";
                     pref = %j;
                     prefName = %this.prefToStore[%j]@%prefAppend;
                     prefValue = 1;
                     command = %this@".done();";
                  };
                  RTBCC_Options_Swatch.add(%box);
                  
                  if(RTBCO_getPref(%this.prefToStore[%j]@%prefAppend) $= 1)
                     %box.setValue(1);
                  
                  %pointer = vectorAdd(%pointer,"0 16");
               }
            }
            %pointer = vectorAdd(%pointer,"0 1");
         }
      }
      %pointer = vectorAdd(%pointer,"0 5");
   }
   RTBCC_Options_Swatch.resize(1,1,194,getWord(%pointer,1));
}

//- RTBCC_Window_Options::onSleep (on sleep callback, saves cc prefs)
function RTBCC_Window_Options::onSleep(%this)
{
}

//- RTBCC_Window_Options::done (saves all the preferences set)
function RTBCC_Window_Options::done(%this)
{
   for(%i=0;%i<RTBCC_Options_Swatch.getCount();%i++)
   {
      %ctrl = RTBCC_Options_Swatch.getObject(%i);
      if(%ctrl.getClassName() $= "GuiCheckboxCtrl")
      {
         if(%ctrl.getValue() $= 1)
            RTBCO_setPref(%ctrl.prefName,%ctrl.prefValue);
         else
            RTBCO_setPref(%ctrl.prefName,0);
      }
      else if(%ctrl.getClassName() $= "GuiRadioCtrl")
      {
         if(%ctrl.getValue() $= 1)
            RTBCO_setPref(%ctrl.prefName,%ctrl.prefValue);
      }
   }
   RTBCO_Save();
   
   if(RTBCC_Socket.authenticated)
      RTBCC_Socket.sendPrefs();
   
   if(RTBCO_getPref("CC::SeparateOffline"))
   {
      %group = RTBCC_Roster.getGroupByName("Offline Users");
      if(%group.getCount() <= 0)
      {
         for(%i=0;%i<RTBCC_Roster.getCount();%i++)
         {
            %group = RTBCC_Roster.getObject(%i);
            if(%group.name $= "Offline Users" || %group.name $= "Your Invites")
               continue;
               
            for(%j=%group.getCount()-1;%j>=0;%j--)
            {
               if(!%group.getObject(%j).online)
                  %group.getObject(%j).moveToGroup("Offline Users");
            }
         }
      }
   }
   else
   {
      %group = RTBCC_Roster.getGroupByName("Offline Users");
      while(%group.getCount() > 0)
         %group.getObject(0).moveToGroup(%group.getObject(0).group.name);
   }
}

//- RTBCC_Window_Splash::onSleep (on sleep callback)
function RTBCC_Window_Splash::onWake(%this)
{
}

//- RTBCC_Window_Splash::onSleep (on sleep callback)
function RTBCC_Window_Splash::onSleep(%this)
{
}

//*********************************************************
//* Register Settings
//*********************************************************
// Groups
RTBCC_Window_Options.registerGroup("General","wrench");
RTBCC_Window_Options.registerGroup("Interface","monitor");
RTBCC_Window_Options.registerGroup("Privacy","user_delete");
RTBCC_Window_Options.registerGroup("Notifications","note");
RTBCC_Window_Options.registerGroup("Special Options","pirate-captain_16");

// Settings
RTBCC_Window_Options.registerPref("Automatically sign me in","General","CC::AutoSignIn","bool");
RTBCC_Window_Options.registerPref("Enable interface sounds","General","CC::EnableSounds","bool");
RTBCC_Window_Options.registerPref("Enable sticky notifications","General","CC::StickyNotifications","bool");
RTBCC_Window_Options.registerPref("Enable chat logging","General","CC::ChatLogging","bool");
RTBCC_Window_Options.registerPref("Separate offline users","Interface","CC::SeparateOffline","bool");
RTBCC_Window_Options.registerPref("Show timestamps in chat","Interface","CC::ShowTimestamps","bool");
RTBCC_Window_Options.registerPref("Save chatroom positions","Interface","CC::SavePositions","bool");
RTBCC_Window_Options.registerPref("People who can send me an invite","Privacy","CC::InviteReq","list:{Anyone:0,People with build trust:1,People with full trust:2,Nobody at all:3}");
RTBCC_Window_Options.registerPref("Allow anyone to see my server","Privacy","CC::ShowServer","bool");
RTBCC_Window_Options.registerPref("Allow anyone to message me","Privacy","CC::AllowPM","bool");
RTBCC_Window_Options.registerPref("Accept server invitations","Privacy","CC::AllowInvites","bool");
RTBCC_Window_Options.registerPref("When someone signs in ...","Notifications","CC::SignIn::","multibool:{Play a beep sound:Beep,Give me a popup message:Note}");
RTBCC_Window_Options.registerPref("When someone messages me ...","Notifications","CC::Message::","multibool:{Play a beep sound:Beep,Give me a popup message:Note}");
RTBCC_Window_Options.registerPref("When someone joins a server ...","Notifications","CC::Join::","multibool:{Play a beep sound:Beep,Give me a popup message:Note}");

// Arr
RTBCC_Window_Options.registerPref("Enable Pirate Scallywag Mode","Special Options","CC::PirateMode","bool",0);

//*********************************************************
//* GUI Modal Utilities
//*********************************************************
//- RTB_ConnectClient::setModalWindow (opens and sets the modal window required)
function RTB_ConnectClient::setModalWindow(%this,%modal)
{
   if(!%this.isOpen())
      RTB_Overlay.push(%this);
   
   %group = RTBCC_Modal;
   
   for(%i=0;%i<%group.getCount();%i++)
   {
      %group.getObject(%i).setVisible(false);
   }
   
   if(%modal $= "")
   {
      %group.setVisible(false);
      return;
   }
   %group.setVisible(true);
   %this.pushToBack(%group);
   
   %name = "RTBCC_Modal_"@%modal;
   if(isObject(%name))
      %name.setVisible(true);
}

//- RTB_ConnectClient::closeModalWindow (closes the modal window)
function RTB_ConnectClient::closeModalWindow(%this)
{
   RTBCC_Modal.setVisible(false);
}

//- RTB_ConnectClient::messageBox (opens a non-closeable message box)
function RTB_ConnectClient::messageBox(%this,%title,%message)
{
   %this.setModalWindow("Box");
   RTBCC_Modal_Box_Title.setText("<color:444444><font:Verdana Bold:12>"@%title);
   RTBCC_Modal_Box_Text.setText("<color:444444><font:Verdana:12>"@%message);
   
   if(RTB_ConnectClient.isOpen())
      RTBCC_Modal_Box_Text.forceReflow();
   %textHeight = getWord(RTBCC_Modal_Box_Text.extent,1);
   %modalHeight = %textHeight+52;
   
   if(%modalHeight < 91)
      %modalHeight = 91;
      
   RTBCC_Modal_Box.resize(11,91,174,%modalHeight);
   RTBCC_Modal_Box.center();
}

//- RTB_ConnectClient::messageBoxOK (opens an ok message box)
function RTB_ConnectClient::messageBoxOK(%this,%title,%message,%ok)
{
   %this.setModalWindow("BoxOK");
   RTBCC_Modal_BoxOK_Title.setText("<color:444444><font:Verdana Bold:12>"@%title);
   RTBCC_Modal_BoxOK_Text.setText("<color:444444><font:Verdana:12>"@%message);
   RTBCC_Modal_BoxOK_Ok.command = "RTB_ConnectClient.closeModalWindow();"@%ok;
   RTBCC_Modal_BoxOK_Ok_Accelerator.makeFirstResponder(1);
   
   if(RTB_ConnectClient.isOpen())
      RTBCC_Modal_BoxOK_Text.forceReflow();
   %textHeight = getWord(RTBCC_Modal_BoxOK_Text.extent,1);
   %modalHeight = %textHeight+52;
   
   if(%modalHeight < 91)
      %modalHeight = 91;
      
   RTBCC_Modal_BoxOK.resize(11,91,174,%modalHeight);
   RTBCC_Modal_BoxOK.center();
}

//- RTB_ConnectClient::messageBoxYesNo (opens a yes/no message box)
function RTB_ConnectClient::messageBoxYesNo(%this,%title,%message,%yes,%no)
{
   %this.setModalWindow("BoxYesNo");
   RTBCC_Modal_BoxYesNo_Title.setText("<color:444444><font:Verdana Bold:12>"@%title);
   RTBCC_Modal_BoxYesNo_Text.setText("<color:444444><font:Verdana:12>"@%message);
   RTBCC_Modal_BoxYesNo_Yes.command = "RTB_ConnectClient.closeModalWindow();"@%yes;
   RTBCC_Modal_BoxYesNo_No.command = "RTB_ConnectClient.closeModalWindow();"@%no;
   
   if(RTB_ConnectClient.isOpen())
      RTBCC_Modal_BoxYesNo_Text.forceReflow();
   %textHeight = getWord(RTBCC_Modal_BoxYesNo_Text.extent,1);
   %modalHeight = %textHeight+52;
   
   if(%modalHeight < 91)
      %modalHeight = 91;
      
   RTBCC_Modal_BoxYesNo.resize(11,91,174,%modalHeight);
   RTBCC_Modal_BoxYesNo.center();
}

//- RTB_ConnectClient::messageBoxError (opens an error message box)
function RTB_ConnectClient::messageBoxError(%this,%title,%message,%ok)
{
   %this.setModalWindow("BoxError");
   RTBCC_Modal_BoxError_Title.setText("<color:444444><font:Verdana Bold:12>"@%title);
   RTBCC_Modal_BoxError_Text.setText("<color:444444><font:Verdana:12>"@%message);
   RTBCC_Modal_BoxError_Ok.command = "RTB_ConnectClient.closeModalWindow();"@%ok;
   RTBCC_Modal_BoxError_Ok_Accelerator.makeFirstResponder(1);
   
   if(RTB_ConnectClient.isOpen())
      RTBCC_Modal_BoxError_Text.forceReflow();
   %textHeight = getWord(RTBCC_Modal_BoxError_Text.extent,1);
   %modalHeight = %textHeight+52;
   
   if(%modalHeight < 91)
      %modalHeight = 91;
      
   RTBCC_Modal_BoxError.resize(11,91,174,%modalHeight);
   RTBCC_Modal_BoxError.center();
}

//- RTBCC_Modal_JoinPassword::send (attemps to join a password server)
function RTBCC_Modal_JoinPassword::send(%this)
{
   if(isObject(ServerConnection))
      disconnect();
      
   MJ_txtIP.setValue($RTB::MCCC::Cache::Joining);
   MJ_txtJoinPass.setValue(RTBCC_Modal_JoinPassword_Pass.getValue());
   MJ_connect();
   
   RTBCC_Modal_JoinPassword_Pass.setValue("");
   RTB_ConnectClient.closeModalWindow();
}

//*********************************************************
//* GUI Interface Implementation
//*********************************************************
//- RTBCC_Window_Roster::addFriend (opens a window to subscribe to a user)
function RTBCC_Window_Roster::addFriend(%this)
{
   RTB_ConnectClient.setModalWindow("AddFriend");
   RTBCC_Modal_AddFriend_BLID.setValue("");
   RTBCC_Modal_AddFriend_BLID.makeFirstResponder(1);
}

//- RTBCC_Modal_AddFriend::close (closes the modal window)
function RTBCC_Modal_AddFriend::close(%this)
{
   RTB_ConnectClient.closeModalWindow();
   RTBCC_Modal_AddFriend_BLID.setValue("");
}

//- RTBCC_Modal_AddFriend::send (attempts to add the specified bl_id as a friend)
function RTBCC_Modal_AddFriend::send(%this)
{
   %bl_id = RTBCC_Modal_AddFriend_BLID.getValue();
   if(%bl_id $= "")
   {
      RTB_ConnectClient.messageBoxOK("Oops ...","You need to enter a Blockland ID into the box to add a friend!","RTBCC_Window_Roster.addFriend();");
      return;
   }
   if(RTBCC_Roster.hasID(%bl_id))
   {
      RTB_ConnectClient.messageBoxOK("Oops ...","You already have that person on your friends list! Try and think of another.","RTBCC_Window_Roster.addFriend();");
      return;
   }
   if(RTBCC_InviteRoster.hasID(%bl_id))
   {
      RTB_ConnectClient.messageBoxOK("Oops ...","You already have a friend invitation from this person. Maybe you should accept that instead.","RTBCC_Window_Roster.addFriend();");
      return;
   }
   if(%bl_id $= RTB_ConnectClient.client_id)
   {
      RTB_ConnectClient.messageBoxOK("Oops ...","You can't add yourself as a friend, that's cheating!","RTBCC_Window_Roster.addFriend();");
      return;
   }
   
   if(mFloatLength(%bl_id, 0) !$= %bl_id)
   {
      RTB_ConnectClient.messageBoxOK("Oops ...","You can only enter numbers for a Blockland ID! Try again ...","RTBCC_Window_Roster.addFriend();");
      return;
   }
   RTBCC_Socket.addToRoster(%bl_id);
   RTB_ConnectClient.messageBox("Hold on a sec ...","Attempting to add "@%bl_id@" to your friends list ...");
}

//- RTBCC_Window_Chat::addRoom (opens a window to create a room)
function RTBCC_Window_Chat::addRoom(%this)
{
   RTB_ConnectClient.messageBoxError("Oops ...","Creating your own chat rooms is not yet available, sorry.<br><br><just:center><bitmap:add-ons/system_Returntoblockland/images/icons/emoticon_unhappy>");
}

//- RTBCC_Window_Chat::refreshRooms (clears & refreshes room list)
function RTBCC_Window_Chat::refreshRooms(%this)
{
   RTBCC_Chat_Swatch.clear();
   
   RTBCC_RoomManager.getRooms();
}

//*********************************************************
//* Connection Handling & Instantiation
//*********************************************************
//- RTBCC_Socket::onConnected (connected callback)
function RTBCC_Socket::onConnected(%this)
{
   //@DEBUG
   if($RTB::Debug)
      echo("\c4>> Connected");
   
   %this.trying = 0;
   %this.connected = 1; 
   
   RTBCC_Roster_Loading.setStage(1);
}

//- RTBCC_Socket::onConnectFailed (connect failed callback)
function RTBCC_Socket::onConnectFailed(%this)
{
   //@DEBUG
   if($RTB::Debug)
      echo("\c2>> Connect Failed");
   
   RTBCC_Roster_Loading.setVisible(false);
   RTB_ConnectClient.messageBoxError("Connection Error","Connection to the RTB Connect server failed. Retrying ...","RTBCC_Socket.softDisconnect();");
   
   %this.retry();
}

//- RTBCC_Socket::onDNSFailed (dns failed callback)
function RTBCC_Socket::onDNSFailed(%this)
{
   //@DEBUG
   if($RTB::Debug)
      echo("\c2>> DNS Failed");
   
   RTBCC_Roster_Loading.setVisible(false);
   RTB_ConnectClient.messageBoxError("Connection Error","Connection to the RTB Connect server failed. Retrying ...","RTBCC_Socket.softDisconnect();");
   
   %this.retry();
}

//- RTBCC_Socket::onLine (on line)
function RTBCC_Socket::onLine(%this,%xml)
{
   %xml = getASCIIString(%xml);
   
   //@DEBUG
   if($RTB::Debug)
      echo("\c2>> ["@getDateTime()@"] "@strReplace(%xml,"\n",""));
   
   %this.parser.bufferData(%xml);
}

//- RTBCC_Socket::onDisconnect (on disconnected)
function RTBCC_Socket::onDisconnect(%this)
{
   //@DEBUG
   if($RTB::Debug)
      echo("\c2>> Disconnected");
   
   %this.tries = 0;
   %this.trying = 0;
   %this.connected = 0;
   RTB_ConnectClient.setStatus("offline");
   RTB_ConnectClient.prepare();
   
   RTBCC_Roster_Loading.setVisible(false);
   RTBCC_Modal.setVisible(false);
   
   if(%this.authenticated && %this.disconnectType $= "")
   {
      RTB_ConnectClient.messageBoxError("Connection Lost","The connection with the server was lost. Retrying ...","RTBCC_Socket.softDisconnect();");
      RTBCC_NotificationManager.push("Connection Lost","Lost connection to server.","delete");
      
      %this.retry();
   }
   %this.disconnectType = "";
   %this.authenticated = 0;
   
   RTBCC_RoomSessionManager.destroy();
}

//*********************************************************
//* Connection Interaction
//*********************************************************
//- RTBCC_Socket::connect (connect to server)
function RTBCC_Socket::connect(%this)
{
   if(%this.connected)
      return;
      
   //@DEBUG
   if($RTB::Debug)
      echo("\c4>> Connecting");
      
   %this.trying = 1;
   RTB_ConnectClient.prepare();
   
   RTBCC_Modal.setVisible(false);
   
   RTBCC_Roster_Loading.setVisible(true);
   RTBCC_Roster_Loading.setStage(0);
   
   %this.roster = RTBCC_createRoster();
   %this.roomManager = RTBCC_createRoomManager();
   %this.inviteRoster = RTBCC_createInviteRoster();
   %this.sessionManager = RTBCC_createSessionManager();
   %this.roomSessionManager = RTBCC_createRoomSessionManager();
   
   Parent::connect(%this,%this.host@":"@%this.port);
}

//- RTBCC_Socket::retry (retries connection)
function RTBCC_Socket::retry(%this)
{
   %this.tries++;
   
   if(!%this.trying)
   {
      %this.trying = 1;
      RTB_ConnectClient.prepare();
   }
   %this.retry = %this.schedule(5000,"connect");
}

//- RTBCC_Socket::softDisconnect (performs a soft disconnect)
function RTBCC_Socket::softDisconnect(%this)
{
   if(isEventPending(%this.retry))
      cancel(%this.retry);
      
   if(%this.connected)
      %this.sendXML("<disconnect />");
   else
      %this.hardDisconnect();
}

//- RTBCC_Socket::hardDisconnect (performs a hard disconnect)
function RTBCC_Socket::hardDisconnect(%this)
{
   %this.disconnectType = "expected";
   
   %this.disconnect();
   %this.onDisconnect();
}

//- RTBCC_Socket::getToken (increments and returns token)
function RTBCC_Socket::getToken(%this)
{
   %letters = "abcdefghijklmnopqrstuvwxyz";
   for(%i=0;%i<4;%i++)
   {
      %token = %token@getSubStr(%letters,getRandom(0,strlen(%letters)-1),1);
   }
   return %token @ %this.token++;
}

//- RTBCC_Socket::sendXML (sends xml stream)
function RTBCC_Socket::sendXML(%this,%xml,%handler,%arg)
{
   if(isObject(%xml))
   {
      if(%xml.attrib["id"] $= "")
      {
         %token = %this.getToken();
         %xml.setAttribute("id",%token);

         if(%handler !$= "")
            %this.setPacketHandler(%token,%handler,%arg);
      }
      %xmlString = %xml.toString();
      
      %xml.delete();
   }
   else
      %xmlString = %xml;
   
   //@DEBUG
   if($RTB::Debug)
      echo("\c4<< ["@getDateTime()@"] "@%xmlString);
   %this.send(getUTF8String(%xmlString)@"\r\n");
   
   return %token;
}

//*********************************************************
//* Incoming BLXS Packet Routing
//*********************************************************
//- RTBCC_Socket::setPacketHandler (registers a packet handler for packets with an id)
function RTBCC_Socket::setPacketHandler(%this,%token,%handler,%arg)
{
   %this.tokenToHandler[%token] = %handler;
   %this.tokenArgument[%token] = %arg;
   
   return 1;
}

//- RTBCC_Socket::onNoticePacket (handles notice packets)
function RTBCC_Socket::onNoticePacket(%this,%parser,%packet)
{
   if(%packet.attrib["from"] $= "system")
   {
      RTB_ConnectClient.messageBoxOK("System Notice",%packet.cData);
      RTBCC_NotificationManager.push("System Notice","Open the overlay to read.","information","",-1);
      return;
   }
   
   if(RTBCC_RoomSessionManager.hasRoom(%packet.attrib["from"]))
      RTBCC_RoomSessionManager.getRoomByName(%packet.attrib["from"]).onNotice(%parser,%packet);
}

//- RTBCC_Socket::onAuthPacket (handles authentication packets)
function RTBCC_Socket::onAuthPacket(%this,%parser,%packet)
{
   if(%packet.attrib["type"] $= "get")
      %this.authenticate();
      
   if(%packet.attrib["type"] $= "result")
   {
      if(%packet.find("success"))
      {
         RTBCC_Roster_Loading.setStage(2);
         %this.authenticated = 1;
         
         RTB_ConnectClient.setStatus("online");
         RTB_ConnectClient.enableInterface();
         RTBCC_Tab_Roster.performClick();
         
         RTBCC_NotificationManager.push("Connected","You are now online.","star");
         
         %this.sendPrefs();
         RTBCC_Roster.load();
      }
      else if(%packet.find("fail"))
      {
         %this.hardDisconnect();
         RTB_ConnectClient.messageBoxError("Login Error",%packet.find("fail").cData);
      }
   }
}

//- RTBCC_Socket::onRosterPacket (handles roster packets)
function RTBCC_Socket::onRosterPacket(%this,%parser,%packet)
{
   if(%packet.attrib["type"] $= "get")
   {
      for(%i=0;%i<%packet.children;%i++)
      {
         %user = %packet.child[%i];
         RTBCC_Roster.addUser(%user);
      }
      
      if(%packet.attrib["end"] $= "1")
      {
         RTBCC_Roster.loading = false;
         RTBCC_Roster.render();
         
         %this.sendPresenceProbe();
         %this.getRoomList();
         
         RTBCC_Roster_Loading.setVisible(false);
      }
   }
   else if(%packet.attrib["type"] $= "add")
   {
      %user = RTBCC_Roster.addUser(%packet.child[0]);
      
      if(!isObject(%user))
         return;
      
      if(%user.getGroup() !$= RTBCC_InviteRoster.getID())
         %user.render();
   }
   else if(%packet.attrib["type"] $= "del")
   {
      %id = %packet.child[0].cData;
      
      if(RTBCC_Roster.hasID(%id))
         RTBCC_Roster.removeByID(%id);
      else if(RTBCC_InviteRoster.hasID(%id))
         RTBCC_InviteRoster.removeByID(%id);
         
      if(RTBCC_SessionManager.hasID(%id))
      {
         %session = RTBCC_SessionManager.getByID(%id);
         %session.writeNotice("You and "@%session.user.name@" are no longer friends.");
      }
   }
   else if(%packet.attrib["type"] $= "block")
   {
      %id = %packet.child[0].cData;
      
      if(RTBCC_Roster.hasID(%id))
      {
         %item = RTBCC_Roster.getByID(%id);
         %item.state = "blocked";
         %item.render();
         
         if(RTBCC_SessionManager.hasID(%id) && RTBCC_SessionManager.getByID(%id).isRendered())
         {
            RTBCC_SessionManager.getByID(%id).writeNotice("You have blocked "@%item.name@".");
            RTBCC_SessionManager.getByID(%id).setBlockedStatus(1);
            RTBCC_SessionManager.getByID(%id).updateStatus(0);
         }
      }
      
      if(RTBCC_TempRoster.hasID(%id))
      {
         %item = RTBCC_TempRoster.getByID(%id);
         %item.state = "blocked";
         
         if(RTBCC_SessionManager.hasID(%id) && RTBCC_SessionManager.getByID(%id).isRendered())
         {
            RTBCC_SessionManager.getByID(%id).writeNotice("You have blocked "@%item.name@".");
            RTBCC_SessionManager.getByID(%id).setBlockedStatus(1);
         }
      }
      
      for(%i=0;%i<RTBCC_RoomSessionManager.getCount();%i++)
      {
         %session = RTBCC_RoomSessionManager.getObject(%i);
         if(%session.manifest.hasUser(%id))
         {
            %user = %session.manifest.getByID(%id);
            %user.blocked = 1;
         }
      }
   }
   else if(%packet.attrib["type"] $= "unblock")
   {
      %id = %packet.child[0].cData;
      
      if(RTBCC_Roster.hasID(%id))
      {
         %item = RTBCC_Roster.getByID(%id);
         %item.state = "";
         %item.render();
         
         if(RTBCC_SessionManager.hasID(%id) && RTBCC_SessionManager.getByID(%id).isRendered())
         {
            RTBCC_SessionManager.getByID(%id).writeNotice("You have unblocked "@%item.name@".");
            RTBCC_SessionManager.getByID(%id).setBlockedStatus(0);
         }
      }
      
      if(RTBCC_TempRoster.hasID(%id))
      {
         %item = RTBCC_TempRoster.getByID(%id);
         %item.state = "";
         
         if(RTBCC_SessionManager.hasID(%id) && RTBCC_SessionManager.getByID(%id).isRendered())
         {
            RTBCC_SessionManager.getByID(%id).writeNotice("You have unblocked "@%item.name@".");
            RTBCC_SessionManager.getByID(%id).setBlockedStatus(0);
         }
      }
      
      for(%i=0;%i<RTBCC_RoomSessionManager.getCount();%i++)
      {
         %session = RTBCC_RoomSessionManager.getObject(%i);
         if(%session.manifest.hasUser(%id))
         {
            %user = %session.manifest.getByID(%id);
            %user.blocked = 0;
         }
      }
   }
}

//- RTBCC_Socket::onPresencePacket (handles presence packets)
function RTBCC_Socket::onPresencePacket(%this,%parser,%packet)
{
   %id = %packet.attrib["from"];
   if(RTBCC_Roster.hasID(%id))
   {
      %user = RTBCC_Roster.getByID(%id);
      
      if(!%user.online && %packet.attrib["type"] $= "online")
      {
         if(!RTB_Overlay.isAwake())
         {
            if(%packet.attrib["event"] !$= "probe" && RTBCO_getPref("CC::SignIn::Beep"))
               alxPlay(RTBCC_OnlineSound);
            if(%packet.attrib["event"] !$= "probe" && RTBCO_getPref("CC::SignIn::Note"))
               RTBCC_NotificationManager.push(%user.name,"has just signed in.","user",%user.id);
         }
            
         if(RTBCO_getPref("CC::SeparateOffline"))
            %user.moveToGroup(%user.group.name);
            
         if(%packet.attrib["event"] !$= "probe" && RTBCC_SessionManager.hasID(%id))
         {
            %session = RTBCC_SessionManager.getByID(%id);
            %session.writeNotice(%user.name@" has signed in.");
         }
      }
      else if (%user.online && %packet.attrib["type"] $= "offline")
      {
         if(RTBCO_getPref("CC::SeparateOffline"))
            %user.moveToGroup("Offline Users");
            
         if(RTBCC_SessionManager.hasID(%id))
         {
            %session = RTBCC_SessionManager.getByID(%id);
            %session.writeNotice(%user.name@" has signed out.");
            %session.updateStatus(0);
            
            %session.setInviteDisplay(0);
            RTBCC_NotificationManager.pop(%id@"_invite");
         }
      }
      
      %user.online = (%packet.attrib["type"] $= "online") ? true : false;
      %user.presence = %packet.find("show").cData;
      
      %status = %packet.find("status").cData;
      if(%user.status !$= "" && !RTB_Overlay.isAwake())
         if((%user.status < 1 || %user.status > 3) && (%status >= 1 && %status <= 3))
         {
            if(RTBCO_getPref("CC::Join::Note"))
               RTBCC_NotificationManager.push(%user.name,"has just started a server.","world",%user.id);
            if(RTBCO_getPref("CC::Join::Beep"))
               alxPlay(RTBCC_JoinSound);
         }
         else if(%user.status < 4 && %status > 3)
         {
            if(RTBCO_getPref("CC::Join::Note"))
               RTBCC_NotificationManager.push(%user.name,"has just joined a server.","world",%user.id);
            if(RTBCO_getPref("CC::Join::Beep"))
               alxPlay(RTBCC_JoinSound);
         }
               
      %user.status = %status;
      
      if(%packet.find("server").children)
      {
         %user.server["ip"] = %packet.find("server/ip").cData;
         %user.server["port"] = %packet.find("server/port").cData;
      }
      else
      {
         %user.server["ip"] = "";
         %user.server["port"] = "";
         
         if(RTBCC_SessionManager.hasID(%id))
         {
            %session = RTBCC_SessionManager.getByID(%id);
            %session.setInviteDisplay(0);
            
            RTBCC_NotificationManager.pop(%id@"_invite");
         }
      }
      
      %user.getGroup().sort();
      %user.rerender();
   }
}

//- RTBCC_Socket::onMessagePacket (handles message packet)
function RTBCC_Socket::onMessagePacket(%this,%parser,%packet)
{
   %to = %packet.attrib["to"];
   %from = %packet.attrib["from"];
   
   if(isInt(%to))
   {
      if(!RTBCC_Roster.hasID(%from))
         RTBCC_TempRoster.addUser(%from,%packet.attrib["name"]);
         
      %session = RTBCC_SessionManager.createSession(%from);
   }
   else
   {
      if(!RTBCC_RoomSessionManager.hasRoom(%to))
         return;
         
      %session = RTBCC_RoomSessionManager.createSession(%to);
   }
   %session.receive(%packet);
}

//- RTBCC_Socket::onActionPacket (handles action packet)
function RTBCC_Socket::onActionPacket(%this,%parser,%packet)
{
   if(%packet.child[0].tag $= "typing")
   {
      %from = %packet.attrib["from"];
      %status = %packet.find("typing/status").cData;
      
      if(RTBCC_SessionManager.hasID(%from))
         %session = RTBCC_SessionManager.getByID(%from).updateStatus(%status);
   }
   else if(%packet.child[0].tag $= "invite")
   {
      %from = %packet.attrib["from"];
      %user = RTBCC_Roster.getByID(%from);
      
      %session = RTBCC_SessionManager.createSession(%from);
      %session.handleInvite(%packet.attrib["ip"],%packet.attrib["port"]);
      
      RTBCC_NotificationManager.push(%user.name,"has invited you to play.","house",%from@"_invite",-1);
      
      %session.focus();
   }
}

//- RTBCC_Socket::onPingPacket (handles ping packet)
function RTBCC_Socket::onPingPacket(%this,%parser,%packet)
{
   %this.sendXML("<pong />");
}

//- RTBCC_Socket::onDisconnectPacket (handles disconnect packet)
function RTBCC_Socket::onDisconnectPacket(%this,%parser,%packet)
{
   %this.hardDisconnect();
   
   if(%packet.attrib["type"] $= "kick")
   {
      RTB_ConnectClient.messageBoxError("You have been kicked!","You have been kicked from the service by an Administrator.");
      RTBCC_NotificationManager.push("Disconnected","You have been kicked.","delete");
   }
   else if(%packet.attrib["type"] $= "timeout")
   {
      RTB_ConnectClient.messageBoxError("Service Timeout","The connection to the service timed out. Retrying ...","RTBCC_Socket.softDisconnect();");
      RTBCC_NotificationManager.push("Disconnected","You have timed out.","delete");
      
      %this.retry();
   }
   else if(%packet.attrib["type"] $= "auth")
   {
      RTB_ConnectClient.messageBoxError("Authentication Failed","RTB was unable to authenticate you, please reconnect.");
      RTBCC_NotificationManager.push("Disconnected","Authentication failed.","delete");
   }
   else if(%packet.attrib["type"] $= "shutdown")
   {
      RTB_ConnectClient.messageBoxError("Service Shutdown","The service has been shut down for maintenance.");
      RTBCC_NotificationManager.push("Service Maintenance","Connect Server going down.","wrench");
   }
   else if(%packet.attrib["type"] $= "reboot")
   {
      RTB_ConnectClient.messageBoxError("Server Restarting","The connect server is being restarted. You'll be automatically re-connected.");
      RTBCC_NotificationManager.push("Service Maintenance","Connect Server rebooting.","time");
      
      %this.retry();
   }
}

//- RTBCC_Socket::onErrorPacket (handles error packet and relays to appropriate handler)
function RTBCC_Socket::onErrorPacket(%this,%parser,%packet)
{
   if(%packet.attrib["id"] !$= "")
   {
      %token = %packet.attrib["id"];
      if(%this.tokenToHandler[%token] !$= "")
      {
         %arg = %this.tokenArgument[%token];
         eval(%this.tokenToHandler[%token]@"("@%this@","@%parser@","@%packet@",\""@%arg@"\");");
         
         %this.tokenArgument[%token] = "";
         %this.tokenToHandler[%token] = "";
      }
   }
}

//*********************************************************
//* BLXS Packet Stanza Constructs & Callback Procedures
//*********************************************************
//- RTBCC_Socket::authenticate (authenticate with connect server)
function RTBCC_Socket::authenticate(%this)
{
   %xml = %this.parser.newElement("auth")
                         .setAttribute("type","set")
                         .setAttribute("version",RTBCC_Socket.version)
                      .newElement("id",RTB_ConnectClient.client_id,1)
                      .newElement("username",RTB_ConnectClient.client_name)
                      .getTop();
                      
   %this.sendXML(%xml);
}

//- RTBCC_Socket::getRoster (retrieves roster from the server)
function RTBCC_Socket::getRoster(%this)
{
   %xml = %this.parser.newElement("roster")
                         .setAttribute("type","get")
                      .getTop();
                      
   %this.sendXML(%xml);
}

//- RTBCC_Socket::sendPresenceProbe (sends presence probe)
function RTBCC_Socket::sendPresenceProbe(%this)
{
   %this.sendPresence(1);
}

//- RTBCC_Socket::sendPresence (sends presence data to server)
function RTBCC_Socket::sendPresence(%this,%probe,%disconnect)
{
   %xml = %this.parser.newElement("presence");
   
   if(%probe)
      %xml.setAttribute("probe",1);
   
   if(isObject(ServerConnection) && !%disconnect)
   {
      %address = ServerConnection.getAddress();
      if(%address $= "local")
      {
         if($Server::LAN)
         {
            if($Server::ServerType $= "Singleplayer")
            {
               %xml = %xml.newElement("status",1,1);
               RTB_ConnectClient.status = 1;
            }
            else
            {
               %xml = %xml.newElement("status",2,1);
               RTB_ConnectClient.status = 2;
            }
            %xml = %xml.newElement("server","",1);
         }
         else
         {
            RTB_ConnectClient.status = 3;
            %xml = %xml.newElement("status",3,1)
                       .newElement("server","")
                       .newElement("ip","x.x.x.x",1)
                       .newElement("port",$Pref::Server::Port,1)
                       .getTop();
         }
      }
      else
      {
         %address = getSubStr(%address,3,strLen(%address));
         %address = strReplace(%address,":"," "); 
         %ip = getWord(%address,0);
         %port = getWord(%address,1);

         if(strPos(%ip,"192.") $= 0 || strPos(%ip,"10.") $= 0)
         {
            %xml = %xml.newElement("status",4,1);
            RTB_ConnectClient.status = 4;
         }
         else
         {
            %xml = %xml.newElement("status",5,1);
            RTB_ConnectClient.status = 5;
         }

        %xml = %xml.newElement("server","")
                   .newElement("ip",%ip,1)
                   .newElement("port",%port,1)
                   .getTop();
      }
   }
   else
   {
      RTB_ConnectClient.status = 0;
      %xml = %xml.getTop().newElement("status",0,1)
                          .newElement("server","",1);
   }
      
   %this.sendXML(%xml);
}

//- RTBCC_Socket::sendStatus (sends user status)
function RTBCC_Socket::sendStatus(%this,%status)
{
   %xml = %xml = %this.parser.newElement("presence")
                             .newElement("show",%status)
                             .getTop();
                             
   %this.sendXML(%xml);
}

//- RTBCC_Socket::addToRoster (adds a user to the roster)
function RTBCC_Socket::addToRoster(%this,%id,%room)
{
   %xml = %this.parser.newElement("roster")
                         .setAttribute("type","add")
                      .newElement("item",%id)
                      .getTop();
                      
   %this.sendXML(%xml,"RTBCC_Socket::onRosterAddResponse",%room);
}

//- RTBCC_Socket::addToRoster (removes a user from the roster)
function RTBCC_Socket::removeFromRoster(%this,%id)
{
   %xml = %this.parser.newElement("roster")
                         .setAttribute("type","del")
                      .newElement("item",%id)
                      .getTop();
                      
   %this.sendXML(%xml);   
}

//- RTBCC_Socket::unblockUser (adds a user to our block list)
function RTBCC_Socket::blockUser(%this,%id)
{
   %xml = %this.parser.newElement("roster")
                         .setAttribute("type","block")
                      .newElement("item",%id)
                      .getTop();
                      
   %this.sendXML(%xml);   
}

//- RTBCC_Socket::unblockUser (removes a user from our block list)
function RTBCC_Socket::unblockUser(%this,%id)
{
   %xml = %this.parser.newElement("roster")
                         .setAttribute("type","unblock")
                      .newElement("item",%id)
                      .getTop();
                      
   %this.sendXML(%xml);   
}

//- RTBCC_Socket::getUserInfo (returns a load of info about a player)
function RTBCC_Socket::getUserInfo(%this,%id)
{
   %xml = %this.parser.newElement("request")
                      .newElement("playerdata",%id)
                      .getTop();
                      
   %this.sendXML(%xml, "RTBCC_Socket::onPlayerInfoResponse");
}

//- RTBCC_Socket::getServerStatus (checks whether server has password or not)
function RTBCC_Socket::getServerStatus(%this,%ip,%port)
{
   %xml = %this.parser.newElement("request")
                      .newElement("gamedata")
                      .newElement("ip",%ip,1)
                      .newElement("port",%port)
                      .getTop();
                      
   %this.sendXML(%xml, "RTBCC_Socket::onServerStatusResponse");
}

//- RTBCC_Socket::sendServerInvite (sends player an invite to your server)
function RTBCC_Socket::sendServerInvite(%this,%to)
{
   %xml = %this.parser.newElement("action")
                      .newElement("invite")
                         .setAttribute("to",%to)
                      .getTop();
                      
   %this.sendXML(%xml, "RTBCC_Socket::onServerInviteResponse");
}

//- RTBCC_Socket::sendMessage (sends a message to a user/room)
function RTBCC_Socket::sendMessage(%this,%to,%message,%action)
{
   %xml = %this.parser.newElement("message")
                         .setAttribute("to",%to)
                      .newElement("body",%message)
                      .getTop();
 
   if(%action)
      %xml.setAttribute("type","action");
                      
   %this.sendXML(%xml,"RTBCC_Socket::onMessageFailed",%to);
}

//- RTBCC_Socket::sendPrefs (sends prefs to be saved on the server)
function RTBCC_Socket::sendPrefs(%this)
{
   %xml = %this.parser.newElement("set")
                      .newElement("prefs")
                      .newElement("inviteMode",RTBCO_getPref("CC::InviteReq"),1)
                      .newElement("showServer",RTBCO_getPref("CC::ShowServer"),1)
                      .newElement("allowPM",RTBCO_getPref("CC::AllowPM"),1)
                      .newElement("allowInvites",RTBCO_getPref("CC::AllowInvites"),1)
                      .newElement("pirateMode",RTBCO_getPref("CC::PirateMode"),1)
                      .getTop();
                      
   %this.sendXML(%xml);
}

//- RTBCC_Socket::sendTypingStatus (sends typing status to user)
function RTBCC_Socket::sendTypingStatus(%this,%to,%status)
{
   %xml = %this.parser.newElement("set")
                         .setAttribute("to",%to)
                      .newElement("typing")
                      .newElement("status",%status)
                      .getTop();
                      
   %this.sendXML(%xml);
}

//- RTBCC_Socket::getRoomList (gets a list of rooms from the server)
function RTBCC_Socket::getRoomList(%this,%update)
{
   %xml = %this.parser.newElement("request")
                      .newElement("roomlist")
                      .getTop();
                      
   %this.sendXML(%xml,"RTBCC_Socket::onRoomListResponse",%update);
}

//- RTBCC_Socket::joinRoom (joins a chatroom)
function RTBCC_Socket::joinRoom(%this,%room)
{
   %xml = %this.parser.newElement("action")
                      .newElement("join",%room)
                      .getTop();
                      
   %this.sendXML(%xml,"RTBCC_Socket::onRoomJoinResponse",%room);
}

//- RTBCC_Socket::leaveRoom (leaves a chatroom)
function RTBCC_Socket::leaveRoom(%this,%room)
{
   %xml = %this.parser.newElement("action")
                      .newElement("leave",%room)
                      .getTop();
                      
   %this.sendXML(%xml);
}

//- RTBCC_Socket::kickUser (kicks a user from a room)
function RTBCC_Socket::kickUser(%this,%room,%user,%reason)
{
   %xml = %this.parser.newElement("action")
                      .newElement("kick")
                         .setAttribute("to",%room)
                         .setAttribute("user",%user)
                         .setAttribute("reason",%reason)
                      .getTop();
                      
   %this.sendXML(%xml,"RTBCC_Socket::onRoomKickResponse",%room);
}

//- RTBCC_Socket::banUser (bans a user from a room)
function RTBCC_Socket::banUser(%this,%room,%user,%reason,%length)
{
   %xml = %this.parser.newElement("action")
                      .newElement("ban")
                         .setAttribute("to",%room)
                         .setAttribute("user",%user)
                         .setAttribute("reason",%reason)
                         .setAttribute("length",%length)
                      .getTop();
                      
   %this.sendXML(%xml,"RTBCC_Socket::onRoomBanResponse",%room);
}

//- RTBCC_Socket::changeUserRank (changes users rank in a room)
function RTBCC_Socket::changeUserRank(%this,%room,%id,%rank)
{
   %xml = %this.parser.newElement("set")
                         .setAttribute("to",%room)
                      .newElement("rank")
                         .setAttribute("user",%id)
                         .setAttribute("level",%rank)
                      .getTop();
                      
   %this.sendXML(%xml,"RTBCC_Socket::onRankChangeError",%room);
}

//*********************************************************
//* Error Callback Handlers
//*********************************************************
//- RTBCC_Socket::onRosterAddResponse (deals with roster add response)
function RTBCC_Socket::onRosterAddResponse(%this,%parser,%packet,%room)
{
   if(isObject(%room))
      %window = %room;
   else
   {
      %window = RTB_ConnectClient;
      
      RTBCC_Modal_AddFriend_BLID.setValue("");
   }
      
   if(%packet.tag $= "success")
      %window.messageBoxOK("Woo!","Your friend request has been sent successfully.");
   else
      %window.messageBoxError("Oops ...",%packet.cData);
}

//- RTBCC_Socket::onMessageFailed (deals with message errors)
function RTBCC_Socket::onMessageFailed(%this,%parser,%packet,%arg)
{
   if(%arg $= "")
      return;
      
   if(isInt(%arg) && RTBCC_SessionManager.hasID(%arg))
      %session = RTBCC_SessionManager.getByID(%arg);
   else if(RTBCC_RoomSessionManager.hasRoom(%arg))
      %session = RTBCC_RoomSessionManager.getRoomByName(%arg);
   else
      return;

   %session.writeError(%packet.cData);
}

//*********************************************************
//* Response Callback Handlers
//*********************************************************
//- RTBCC_Socket::onServerStatusResponse (deals with server response)
function RTBCC_Socket::onServerStatusResponse(%this,%parser,%packet)
{
   if(%packet.tag $= "response")
   {
      $ServerInfo::MaxPlayers = %packet.attrib["maxPlayers"];
      $ServerInfo::Name = %packet.attrib["name"];
      
      RTB_ConnectClient.closeModalWindow();
      if(%packet.attrib["type"] $= "password")
      {
         RTB_ConnectClient.setModalWindow("JoinPassword");
         RTBCC_Modal_JoinPassword_Pass.setValue("");
         RTBCC_Modal_JoinPassword_Pass.makeFirstResponder(1);
      }
      else
      {
         if(isObject(ServerConnection))
            disconnect();
            
         MJ_txtIP.setValue($RTB::MCCC::Cache::Joining);
         MJ_txtJoinPass.setValue("");
         MJ_connect();
      }
   }
   else if(%packet.tag $= "error")
      RTB_ConnectClient.messageBoxError("Unable to Join",%packet.cData);
}

//- RTBCC_Socket::onServerInviteResponse (deals with server response)
function RTBCC_Socket::onServerInviteResponse(%this,%parser,%packet)
{
   if(%packet.tag $= "success")
      RTB_ConnectClient.messageBoxOk("Success","Your invite has been sent.");
   else if(%packet.tag $= "error")
      RTB_ConnectClient.messageBoxError("Invite Failed",%packet.cData);
}

//- RTBCC_Socket::onRankChangeError (deals with rank change errors)
function RTBCC_Socket::onRankChangeError(%this,%parser,%packet,%room)
{
   %room = RTBCC_RoomSessionManager.getRoomByName(%room);
   %room.writeError(%packet.cData);
}

//- RTBCC_Socket::onRoomListResponse (list of rooms from server)
function RTBCC_Socket::onRoomListResponse(%this,%parser,%packet,%update)
{
   %rooms = %packet.getObject(0);
   for(%i=0;%i<%rooms.children;%i++)
   {
      %room = %rooms.getObject(%i);
      RTBCC_RoomManager.addRoom(%room.cData,%room.attrib["icon"],%room.attrib["type"],%room.attrib["owner"],%room.attrib["users"],%update);
   }
   
   if(!RTBCC_RoomManager.hasList)
      RTBCC_RoomManager.performAutoJoins();
      
   RTBCC_RoomManager.hasList = 1;
   
   if(!%update)
      RTBCC_RoomManager.render();
}

//- RTBCC_Socket::onRoomJoinResponse (room join response)
function RTBCC_Socket::onRoomJoinResponse(%this,%parser,%packet,%room)
{
   RTB_ConnectClient.closeModalWindow();
   
   if(%packet.tag $= "success")
   {
      %roomSO = RTBCC_RoomSessionManager.createSession(%room);
      
      if(RTB_ConnectClient.isOpen() && RTB_ConnectClient.currPane $= "RTBCC_Window_Chat")
         RTBCC_RoomManager.refresh();
      
      RTBCC_NotificationManager.push(%room,"You have joined the room.","comment_add","join_"@%room);
      
      %roomSO.manifest.loading = true;
   }
   else if(%packet.tag $= "error")
   {
      if(%packet.attrib["type"] $= "banned")
      {
         RTB_ConnectClient.messageBoxError(%room,"You are banned from this room.<br><br><font:Verdana Bold:12>Reason: <font:Verdana:12>"@%packet.attrib["reason"]@"<br><font:Verdana Bold:12>Remaining: <font:Verdana:12>"@timeDiffString(%packet.attrib["left"])@" left.");
      }
      else
         RTB_ConnectClient.messageBoxError(%room,%packet.cData);
   }
}

//- RTBCC_Socket::onRoomKickResponse (room kick response)
function RTBCC_Socket::onRoomKickResponse(%this,%parser,%packet,%room)
{
   if(%packet.tag $= "error")
   {
      %room = RTBCC_RoomSessionManager.getRoomByName(%room);
      %room.writeError(%packet.cData);
   }
}

//- RTBCC_Socket::onRoomBanResponse (room ban response)
function RTBCC_Socket::onRoomBanResponse(%this,%parser,%packet,%room)
{
   if(%packet.tag $= "error")
   {
      %room = RTBCC_RoomSessionManager.getRoomByName(%room);
      %room.writeError(%packet.cData);
   }
}

//- RTBCC_Socket::onPlayerInfoResponse (info about specific player)
function RTBCC_Socket::onPlayerInfoResponse(%this,%parser,%packet)
{
   if(%packet.tag $= "error")
   {
      RTB_ConnectClient.messageBoxError("No Information",%packet.cData);
      return;
   }
   
   %id = %packet.find("id").cData;
   %name = %packet.find("name").cData;
   %status = %packet.find("status").cData;
   %lastOnline = (%packet.find("last_online").cData $= "-1") ? "Unknown" : timeDiffString(%packet.find("last_online").cData,0) SPC "ago";
   
   switch(%status)
   {
      case 0:
         %textStatus = "Online";
      case 1:
         %textStatus = "Singleplayer";
      case 2:
         %textStatus = "Hosting LAN";
      case 3:
         %textStatus = "Hosting";
      case 4:
         %textStatus = "Playing LAN";
      case 5:
         %textStatus = "Playing";
      default:
         %textStatus = "Offline";
   }
   
   RTB_ConnectClient.setModalWindow("PlayerInfo");
   %swatch = RTBCC_Modal_PlayerInfo_Swatch;
   %swatch.clear();
   
   %bitmap = new GuiBitmapCtrl()
   {
      position = "5 6";
      extent = "16 16";
      bitmap = $RTB::Path@"images/icons/information";
   };
   %swatch.add(%bitmap);
   
   %text = new GuiMLTextCtrl()
   {
      position = "25 8";
      extent = "140 12";
      text = "<color:444444><font:Verdana Bold:12>Details";
      selectable = false;
   };
   %swatch.add(%text);
   
   %dots = new GuiBitmapCtrl()
   {
      position = "5 25";
      extent = "158 2";
      bitmap = $RTB::Path@"images/ui/dottedLine";
      wrap = true;
   };
   %swatch.add(%dots);
   
   %bl_idText = new GuiMLTextCtrl()
   {
      position = "6 31";
      extent = "156 12";
      text = "<color:444444><font:Verdana Bold:12>BL ID<just:right><font:Verdana:12>"@%id;
      selectable = false;
   };
   %swatch.add(%bl_idText);
   
   %nameText = new GuiMLTextCtrl()
   {
      position = "6 47";
      extent = "156 12";
      text = "<color:444444><font:Verdana Bold:12>Name<just:right><font:Verdana:12>"@%name;
      selectable = false;
   };
   %swatch.add(%nameText);
   
   %statusText = new GuiMLTextCtrl()
   {
      position = "6 63";
      extent = "156 12";
      text = "<color:444444><font:Verdana Bold:12>Status<just:right><font:Verdana:12>"@%textStatus;
      selectable = false;
   };
   %swatch.add(%statusText);
   
   if(%status $= "-1")
   {
      %lastOnlineText = new GuiMLTextCtrl()
      {
         position = "6 79";
         extent = "156 12";
         text = "<color:444444><font:Verdana Bold:12>Last Online<just:right><font:Verdana:12>"@%lastOnline;
         selectable = false;
      };
      %swatch.add(%lastOnlineText);
      
      RTBCC_Modal_PlayerInfo_Swatch.extent = "168 100";
      RTBCC_Modal_PlayerInfo.resize(12,62,174,153);
      RTBCC_Modal_PlayerInfo.center();
      return;
   }
   
   if(%status $= "0" || %status $= "1" || %status $= "2" || %status $= "4" || %packet.find("server/no_details"))
   {
      RTBCC_Modal_PlayerInfo_Swatch.extent = "168 80";
      RTBCC_Modal_PlayerInfo.resize(12,72,174,133);
      RTBCC_Modal_PlayerInfo.center();
      return;
   }
   
   %bitmap = new GuiBitmapCtrl()
   {
      position = "5 87";
      extent = "16 16";
      bitmap = $RTB::Path@"images/icons/world";
   };
   %swatch.add(%bitmap);
   
   %text = new GuiMLTextCtrl()
   {
      position = "25 89";
      extent = "140 12";
      text = "<color:444444><font:Verdana Bold:12>Server Details";
      selectable = false;
   };
   %swatch.add(%text);
   
   %dots = new GuiBitmapCtrl()
   {
      position = "5 106";
      extent = "158 2";
      bitmap = $RTB::Path@"images/ui/dottedLine";
      wrap = true;
   };
   %swatch.add(%dots);
   
   %name = %packet.find("server/name").cData;
   %host = %packet.find("server/host").cData;
   %players = %packet.find("server/players").cData;
   %maxPlayers = %packet.find("server/maxPlayers").cData;
   %bricks = numberFormat(%packet.find("server/bricks").cData);
   %map = %packet.find("server/map").cData;
   %password = %packet.find("server/password").cData;
   %dedicated = %packet.find("server/dedicated").cData;
   
   if(%dedicated)
   {
      %bitmap = new GuiBitmapCtrl()
      {
         position = "146 87";
         extent = "16 16";
         bitmap = $RTB::Path@"images/icons/server";
      };
      %swatch.add(%bitmap);
   }
   
   %nameLabel = new GuiMLTextCtrl()
   {
      position = "6 112";
      extent = "156 12";
      text = "<color:444444><font:Verdana Bold:12>Name";
      selectable = false;
   };
   %swatch.add(%nameLabel);
   
   %nameText = new GuiMLTextCtrl()
   {
      position = "39 112";
      extent = "123 12";
      text = "<color:444444><just:right><font:Verdana:12>"@%name;
      selectable = false;
   };
   %swatch.add(%nameText);
   %nameText.fitText(%nameText.text);
   
   %hostText = new GuiMLTextCtrl()
   {
      position = "6 128";
      extent = "156 12";
      text = "<color:444444><font:Verdana Bold:12>Host<just:right><font:Verdana:12>"@%host;
      selectable = false;
   };
   %swatch.add(%hostText);
   
   %playersText = new GuiMLTextCtrl()
   {
      position = "6 144";
      extent = "156 12";
      text = "<color:444444><font:Verdana Bold:12>Players<just:right><font:Verdana:12>"@%players SPC "/" SPC %maxPlayers;
      selectable = false;
   };
   %swatch.add(%playersText);
   
   %bricksText = new GuiMLTextCtrl()
   {
      position = "6 160";
      extent = "156 12";
      text = "<color:444444><font:Verdana Bold:12>Bricks<just:right><font:Verdana:12>"@%bricks;
      selectable = false;
   };
   %swatch.add(%bricksText);
   
   %mapText = new GuiMLTextCtrl()
   {
      position = "6 176";
      extent = "156 12";
      text = "<color:444444><font:Verdana Bold:12>Map<just:right><font:Verdana:12>"@%map;
      selectable = false;
   };
   %swatch.add(%mapText);
   
   if(%password)
   {
      %bitmap = new GuiBitmapCtrl()
      {
         position = "143 87";
         extent = "16 16";
         bitmap = $RTB::Path@"images/icons/lock";
      };
      %swatch.add(%bitmap);
   }
   
   RTBCC_Modal_PlayerInfo_Swatch.extent = "168 190";
   RTBCC_Modal_PlayerInfo.resize(12,72,174,245);
   RTBCC_Modal_PlayerInfo.center();
}

//*********************************************************
//* Roster Initialisation & Implementation
//*********************************************************
//- RTBCC_createRoster (creates a friend roster object)
function RTBCC_createRoster()
{
   if(isObject(RTBCC_Roster))
      RTBCC_Roster.delete();
      
   %roster = new ScriptGroup(RTBCC_Roster)
   {
      loading = true;
   };
   RTBGroup.add(%roster);
   
   %roster.addGroup("Your Invites","folder_heart");
   %roster.addGroup("Offline Users","folder");
   
   return %roster;
}

//- RTBCC_Roster::load (loads roster from server)
function RTBCC_Roster::load(%this)
{
   %this.loading = true;
   
   RTBCC_Socket.getRoster();
}

//- RTBCC_Roster::addUser (adds a user to the roster)
function RTBCC_Roster::addUser(%this,%user)
{
   %id = %user.attrib["id"];
   %name = %user.attrib["name"];
   %state = %user.attrib["state"];
   
   if(%state $= "pending_from")
   {
      if(%user = RTBCC_InviteRoster.addUser(%user))
      {
         RTBCC_NotificationManager.push(%user.name,"wants to be your friend.","heart_add",%user.id@"_friend",-1);
         return %user;
      }
      else
         return false;
   }
   
   if(%this.hasID(%id))
      %this.removeByID(%id);
      
   if(%state $= "pending_to")
      %group = %this.addGroup("Your Invites","folder_heart");
   else
      %group = %this.addGroup("Friends");
      
   if(RTBCO_getPref("CC::SeparateOffline") && %group.name !$= "Your Invites")
   {
      %user = %this.addGroup("Offline Users").addUser(%id,%name);
   }
   else
      %user = %group.addUser(%id,%name);
      
   if(RTBCC_TempRoster.hasID(%id))
   {
      RTBCC_TempRoster.removeByID(%id);
      if(RTBCC_SessionManager.hasID(%id))
      {
         %session = RTBCC_SessionManager.getByID(%id);
         %session.user = %user;
      }
   }
      
   %user.group = %group;
   %user.state = %state;
   
   return %user;
}

//- RTBCC_Roster::addGroup (adds a group object to the roster)
function RTBCC_Roster::addGroup(%this,%name,%icon)
{
   if(%this.hasGroup(%name))
      return %this.getGroupByName(%name);
      
   %group = new ScriptGroup()
   {
      class = "RTBCC_RosterGroup";
      
      name = %name;
      icon = "folder_user";
   };
   %this.add(%group);
   
   if(%icon !$= "")
      %group.icon = %icon;
   
   if(%this.getCount() $= 1)
      %this.render();
      
   return %group;
}

//- RTBCC_Roster::getUserCount (gets total users in the roster)
function RTBCC_Roster::getUserCount(%this)
{
   %users = 0;
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      %users += %group.getCount();
   }
   return %users;
}

//- RTBCC_Roster::hasID (checks if id is present in roster)
function RTBCC_Roster::hasID(%this,%id)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      if(%group.hasID(%id))
         return true;
   }
   return false;
}

//- RTBCC_Roster::getByID (returns user based on id)
function RTBCC_Roster::getByID(%this,%id)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      if(%group.hasID(%id))
         return %group.getByID(%id);
   }
   return false;
}

//- RTBCC_Roster::removeByID (removes a user based on id)
function RTBCC_Roster::removeByID(%this,%id)
{
   if(!%this.hasID(%id))
      return false;

   %user = %this.getByID(%id);
   %tempUser = RTBCC_TempRoster.addUser(%id,%user.name);
   if(RTBCC_SessionManager.hasID(%id))
   {
      %session = RTBCC_SessionManager.getByID(%id);
      %session.user = %tempUser;
   }      
   %group = %user.getGroup();
   %user.unrender();
   %user.delete();
   
   if(%group.getCount() <= 0)
   {
      %this.removeGroupByName(%group.name);
   }
   return true;
}

//- RTBCC_Roster::hasGroup (checks if roster has group by name)
function RTBCC_Roster::hasGroup(%this,%name)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      if(%group.name $= %name)
         return true;
   }
   return false;
}

//- RTBCC_Roster::getGroupByName (returns group object by name)
function RTBCC_Roster::getGroupByName(%this,%name)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      if(%group.name $= %name)
         return %group;
   }
   return false;
}

//- RTBCC_Roster::removeGroupByName (removes a group object by name)
function RTBCC_Roster::removeGroupByName(%this,%name)
{
   if(!%this.hasGroup(%name))
      return false;
      
   %group = %this.getGroupByName(%name);
   %group.unrender();
   %group.delete();
   
   if(%this.getCount() <= 0)
      %this.render();
      
   return true;
}

//*********************************************************
//* Roster Group Initialisation & Implementation
//*********************************************************
//- RTBCC_RosterGroup::addUser (adds a user object to roster group)
function RTBCC_RosterGroup::addUser(%this,%id,%name)
{
   if(%this.hasID(%id))
      return %this.getByID(%id);
      
   %user = new ScriptObject()
   {
      class = "RTBCC_RosterUser";
      
      id = %id;
      name = %name;
      group = %this;
      groupName = %this.name;
   };
   %this.add(%user);
   
   return %user;
}

//- RTBCC_RosterGroup::hasID (checks if group has user by id)
function RTBCC_RosterGroup::hasID(%this,%id)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %user = %this.getObject(%i);
      if(%user.id $= %id)
         return true;
   }
   return false;
}

//- RTBCC_RosterGroup::getByID (returns user by id)
function RTBCC_RosterGroup::getByID(%this,%id)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %user = %this.getObject(%i);
      if(%user.id $= %id)
         return %user;
   }
   return false;
}

//- RTBCC_RosterGroup::removeByID (removes user by id)
function RTBCC_RosterGroup::removeByID(%this,%id)
{
   if(!%this.hasID(%id))
      return false;
      
   %this.remove(%this.getByID(%id));
   
   if(%this.isRendered())
      %this.rerender();
}

//*********************************************************
//* Roster User Initialisation & Implementation
//*********************************************************
function RTBCC_RosterUser::moveToGroup(%this,%name)
{      
   if(!RTBCC_Roster.hasGroup(%name))
      %group = RTBCC_Roster.addGroup(%name);
   else
      %group = RTBCC_Roster.getGroupByName(%name);
      
   %this.getGroup().removeByID(%this.id);
   %group.add(%this);
   %this.rerender();
}

//*********************************************************
//* Temp Roster Initialisation & Implementation
//*********************************************************
//- RTBCC_createTempRoster (creates a temporary user roster object)
function RTBCC_createTempRoster()
{
   if(isObject(RTBCC_TempRoster))
      RTBCC_TempRoster.delete();
      
   %roster = new ScriptGroup(RTBCC_TempRoster);
   RTBGroup.add(%roster);
   
   return %roster;
}

//- RTBCC_TempRoster::addUser (adds a user to the roster)
function RTBCC_TempRoster::addUser(%this,%id,%name)
{
   if(%this.hasID(%id))
      return %this.getByID(%id);
      
   %user = new ScriptObject()
   {
      class = "RTBCC_TempRosterUser";
      
      id = %id;
      name = %name;
   };
   %this.add(%user);
   
   return %user;
}

//- RTBCC_TempRoster::hasID (checks for a user by id)
function RTBCC_TempRoster::hasID(%this,%id)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      if(%this.getObject(%i).id $= %id)
         return true;
   }
   return false;
}

//- RTBCC_TempRoster::getByID (returns a user by id)
function RTBCC_TempRoster::getByID(%this,%id)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %user = %this.getObject(%i);
      if(%user.id $= %id)
         return %user;
   }
   return false;
}

//- RTBCC_TempRoster::removeByID (removes a user by id)
function RTBCC_TempRoster::removeByID(%this,%id)
{
   if(!%this.hasID(%id))
      return false;
      
   %user = %this.getByID(%id);
   %user.delete();
   
   return true;
}

//*********************************************************
//* Invite Roster Initialisation & Implementation
//*********************************************************
//- RTBCC_createInviteRoster (creates the invitation roster)
function RTBCC_createInviteRoster()
{
   if(isObject(RTBCC_InviteRoster))
      RTBCC_InviteRoster.delete();
      
   %roster = new ScriptGroup(RTBCC_InviteRoster);
   RTBGroup.add(%roster);
   
   %roster.render();
   
   return %roster;
}

//- RTBCC_InviteRoster::addUser (adds a user to the invite roster)
function RTBCC_InviteRoster::addUser(%this,%user)
{
   %id = %user.attrib["id"];
   %name = %user.attrib["name"];
   
   if(%this.hasID(%id))
      return %this.getByID(%id);

   if($Trust::Count $= "")
      loadTrustList();    
      
   if(RTBCO_getPref("CC::InviteReq") $= 3)
   {
      RTBCC_Socket.removeFromRoster(%id);
      return false;
   }
   else if(RTBCO_getPref("CC::InviteReq") $= 2)
   {
      for(%i=0;%i<$Trust::Count;%i++)
      {
         %trust = $Trust::Line[%i];
         if(%id $= getField(%trust,0))
         {
            if(getField(%trust,1) $= "2")
            {
               %match = true;
               break;
            }
         }
      }
      
      if(!%match)
      {
         RTBCC_Socket.removeFromRoster(%id);
         return false;
      }
   }
   else if(RTBCO_getPref("CC::InviteReq") $= 1)
   {
      for(%i=0;%i<$Trust::Count;%i++)
      {
         %trust = $Trust::Line[%i];
         if(%id $= getField(%trust,0))
         {
            if(getField(%trust,1) $= "1" || getField(%trust,1) $= "2")
            {
               %match = true;
               break;
            }
         }
      }
      
      if(!%match)
      {
         RTBCC_Socket.removeFromRoster(%id);
         return false;
      }
   }
      
   %user = new ScriptObject()
   {
      class = "RTBCC_InviteRosterItem";
      
      id = %id;
      name = %name;
   };
   %this.add(%user);
   
   %this.render();
   
   return %user;
}

//- RTBCC_InviteRoster::hasID (checks for an invite by id)
function RTBCC_InviteRoster::hasID(%this,%id)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      if(%this.getObject(%i).id $= %id)
         return true;
   }
   return false;
}

//- RTBCC_InviteRoster::getByID (returns an invite by id)
function RTBCC_InviteRoster::getByID(%this,%id)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %user = %this.getObject(%i);
      if(%user.id $= %id)
         return %user;
   }
   return false;
}

//- RTBCC_InviteRoster::removeByID (removes an invite by id)
function RTBCC_InviteRoster::removeByID(%this,%id)
{
   if(!%this.hasID(%id))
      return false;
      
   %user = %this.getByID(%id);
   %user.delete();
   
   %this.render();
   
   return true;
}

//*********************************************************
//* Invite Roster Runtime Manipulation
//*********************************************************
//- RTBCC_InviteRoster::accept (accepts a roster invite request)
function RTBCC_InviteRoster::accept(%this,%invite)
{
   RTBCC_Socket.addToRoster(%invite.id);
   %this.removeByID(%invite.id);
}

//- RTBCC_InviteRoster::accept (rejects a roster invite request)
function RTBCC_InviteRoster::reject(%this,%invite,%block)
{
   if(%block $= "")
   {
      RTB_ConnectClient.messageBoxYesNo("Okay, but how about ...","Would you like to block this user from sending you future friend requests?",%this@".reject("@%invite@",1);",%this@".reject("@%invite@",0);");
      return;
   }
   
   if(%block)
      RTBCC_Socket.blockUser(%invite.id);
   
   RTBCC_Socket.removeFromRoster(%invite.id);
   %this.removeByID(%invite.id);
}

//*********************************************************
//* Roster Live Rendering & Manipulation
//*********************************************************
//- RTBCC_Roster_Swatch::clear (resizes the swatch to the correct extent)
function RTBCC_Roster_Swatch::clear(%this)
{
   Parent::clear(%this);
   %this.resize(1,1,194,getWord(RTBCC_Roster_Scroll.extent,1)-2);
}

//- RTBCC_Roster_Swatch::reshape (shapes the size of the roster swatch to be high enough)
function RTBCC_Roster_Swatch::reshape(%this)
{
   if(%this.getLowestPoint() < (getWord(RTBCC_Roster_Scroll.extent,1)-2))
      %this.resize(1,getWord(%this.position,1),194,getWord(RTBCC_Roster_Scroll.extent,1)-2);
   else
      %this.resize(1,getWord(%this.position,1),194,%this.getLowestPoint());
}

//- RTBCC_Roster::render (renders the entire roster)
function RTBCC_Roster::render(%this)
{
   RTBCC_Roster_Swatch.clear();
   
   if(%this.getUserCount() <= 0)
   {
      %text = new GuiMLTextCtrl()
      {
         horizSizing = "center";
         vertSizing = "center";
         
         extent = "194 40";
         text = "<just:center><bitmap:" @ $RTB::Path @ "images/icons/emoticon_unhappy><br><br><color:444444><font:Verdana:12>You don't have any friends!";
         
         selectable = false;
      };
      RTBCC_Roster_Swatch.add(%text);
      RTBCC_Roster_Swatch.reshape();
      if(RTB_ConnectClient.isOpen())
         %text.forceReflow();
      %text.center();
      return;
   }
   
   %this.sort();
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      if(%group.getCount() <= 0)
         continue;
         
      %group.render();
   }
}

//- RTBCC_Roster::sort (sorts child object indexes within group by child name value)
function RTBCC_Roster::sort(%this)
{
   if(%this.getCount() <= 0)
      return;
      
   %sorter = new GuiTextListCtrl();
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      %sorter.addRow(%group,%group.name);
   }
   %sorter.sort(0,1);
   
   for(%i=0;%i<%sorter.rowCount();%i++)
   {
      %this.pushToBack(%sorter.getRowId(%i));
   }
   %sorter.delete();
   
   %this.pushToBack(%this.getGroupByName("Your Invites"));
   %this.pushToBack(%this.getGroupByName("Offline Users"));
}

//- RTBCC_RosterUser::isRendered (checks to see if the roster user is rendered already)
function RTBCC_RosterUser::isRendered(%this)
{
   if(!isObject(%this.gui_container))
      return false;
      
   if(%this.gui_container.getGroup().getID() !$= %this.getGroup().container.getID())
      return false;
      
   return true;
}

//- RTBCC_RosterUser::render (attempts to render the roster user)
function RTBCC_RosterUser::render(%this)
{
   if(%this.isRendered())
   {
      %image = "status_offline";
      
      switch(%this.status)
      {
         case 1:
            %textStatus = "Singleplayer";
         case 2:
            %textStatus = "Hosting LAN";
         case 3:
            %textStatus = "Hosting";
         case 4:
            %textStatus = "Playing LAN";
         case 5:
            %textStatus = "Playing";
      }
         
      if(%this.online $= true)
         if(%this.presence $= "away")
            %image = "status_away";
         else if(%this.presence $= "busy")
            %image = "status_busy";
         else
            %image = "status_online";
         
      if(%this.state $= "blocked")
         if(%this.online)
            %image = "status_online_blocked";
         else
            %image = "status_offline_blocked";
            
      if(%this.state $= "blocked")
         %textStatus = "Blocked";
         
      %this.gui_rosterStatus.setBitmap($RTB::Path @ "images/icons/" @ %image);
      %this.gui_rosterName.setText("<color:444444><font:Verdana:12>" @ %this.name @ "<just:right><font:Verdana Italic:12>" @ %textStatus);
      
      if(isObject(%this.gui_menu))
         %this.closeRosterMenu();
   }
   else
   {
      if(!%this.getGroup().isRendered())
         %this.getGroup().render();
      else
      {
         %position = 19;
         %this.getGroup().sort();
         for(%i=0;%i<%this.getGroup().getCount();%i++)
         {
            %user = %this.getGroup().getObject(%i);
            if(!%user.isRendered() && %user !$= %this)
               continue;
               
            if(%this $= %user)
            {
               %this.getGroup().container.conditionalShiftY(%position,22);
               %this.renderInPlace("13" SPC %position);
               %this.getGroup().container.extent = vectorAdd(%this.getGroup().container.extent,"0 22");
               RTBCC_Roster_Swatch.conditionalShiftY(getWord(%this.getGroup().container.position,1) + 1,22);
            }
            else
               %position += 22;
         }
         RTBCC_Roster_Swatch.reshape();
      }
   }
}

//- RTBCC_RosterUser::renderInPlace (renders a roster user taking a position argument)
function RTBCC_RosterUser::renderInPlace(%this,%position)
{
   if(%this.isRendered())
      return;
      
   %swatch = %this.getGroup().container;
   
   %container = new GuiSwatchCtrl()
   {
      position = %position;
      extent = "170 22";
      
      color = "0 0 0 0";
   };
   %swatch.add(%container);
   %this.gui_container = %container;
   
   %selectBox = new GuiBitmapCtrl()
   {
      position = "0 0";
      extent = "170 22";
      
      visible = false;
      bitmap = $RTB::Path @ "images/ui/buddyListSelect_n";
   };
   %container.add(%selectBox);
   %this.gui_selectBox = %selectBox;
   
   %icon = new GuiBitmapCtrl()
   {
      position = "1 3";
      extent = "16 16";
   };
   %container.add(%icon);
   %this.gui_rosterStatus = %icon;
   
   %text = new GuiMLTextCtrl()
   {
      position = "19 5";
      extent = "147 12";
      
      selectable = false;
   };
   %container.add(%text);
   %this.gui_rosterName = %text;
   
   %mouseEvent = new GuiMouseEventCtrl()
   {
      position = "0 0";
      extent = "170 22";
      
      persistent = 1;
      eventType = "BuddyListSelect";
      eventCallbacks = "1111011";
      
      user = %this;
      select = %selectBox;
   };
   %container.add(%mouseEvent);
   %this.gui_mouseEvent = %mouseEvent;
   
   %this.render();
}

//- RTBCC_RosterUser::rerender (rerenders the roster user if it's already rendered)
function RTBCC_RosterUser::rerender(%this)
{
   if(%this.isRendered())
      %this.unrender();
   %this.render();
}

//- RTBCC_RosterUser::unrender (unrenders the roster user and parent group if empty)
function RTBCC_RosterUser::unrender(%this)
{
   if(!%this.isRendered())
      return;
      
   %this.closeRosterMenu();
   %position = getWord(%this.gui_container.position,1);

   %this.gui_container.delete();

   %this.getGroup().container.conditionalShiftY(%position,-22);
   %this.getGroup().container.extent = vectorSub(%this.getGroup().container.extent,"0 22");
   RTBCC_Roster_Swatch.conditionalShiftY(getWord(%this.getGroup().container.position,1)+1,-22);
   
   RTBCC_Roster_Swatch.reshape();
}

//- RTBCC_RosterGroup::isRendered (checks to see if the roster group is rendered already)
function RTBCC_RosterGroup::isRendered(%this)
{
   if(!isObject(%this.container))
      return false;
      
   if(!isObject(%this.container.getGroup()) || %this.container.getGroup().getID() !$= RTBCC_Roster_Swatch.getID())
      return false;
      
   return true;
}

//- RTBCC_RosterGroup::sort (sorts child object indexes within group by child name value)
function RTBCC_RosterGroup::sort(%this)
{
   if(%this.getCount() <= 0)
      return;
      
   %sorter = new GuiTextListCtrl();
   for(%i=0;%i<%this.getCount();%i++)
   {
      %user = %this.getObject(%i);
      if(%user.online && %user.status > 0)
         %sorter.addRow(%user,%user.name);
   }
   %sorter.sort(0,1);
   
   for(%i=0;%i<%sorter.rowCount();%i++)
   {
      %this.pushToBack(%sorter.getRowId(%i));
   }
   %sorter.delete();
   
   %sorter = new GuiTextListCtrl();
   for(%i=0;%i<%this.getCount();%i++)
   {
      %user = %this.getObject(%i);
      if(%user.online && %user.status <= 0)
         %sorter.addRow(%user,%user.name);
   }
   %sorter.sort(0,1);
   
   for(%i=0;%i<%sorter.rowCount();%i++)
   {
      %this.pushToBack(%sorter.getRowId(%i));
   }
   %sorter.delete();
   
   %sorter = new GuiTextListCtrl();
   for(%i=0;%i<%this.getCount();%i++)
   {
      %user = %this.getObject(%i);
      if(!%user.online)
         %sorter.addRow(%user,%user.name);
   }
   %sorter.sort(0,1);
   
   for(%i=0;%i<%sorter.rowCount();%i++)
   {
      %this.pushToBack(%sorter.getRowId(%i));
   }
   %sorter.delete();
}

//- RTBCC_RosterGroup::render (attempts to render the roster group and user items)
function RTBCC_RosterGroup::render(%this)
{
   if(%this.isRendered())
      return;
      
   if(%this.getCount() <= 0)
      return;
      
   %position = 0;
   RTBCC_Roster.sort();
   for(%i=0;%i<RTBCC_Roster.getCount();%i++)
   {
      %group = RTBCC_Roster.getObject(%i);
      if(!%group.isRendered() && %this !$= %group)
         continue;
         
      %group.sort();
      if(%this $= %group)
      {
         %extent = ((%group.getCount() + 1) * 22) + 4;
         RTBCC_Roster_Swatch.conditionalShiftY(%position,%extent);
         
         %group.renderInPlace("0" SPC %position);
         
         for(%j=0;%j<%group.getCount();%j++)
         {
            %position = "13" SPC (%j * 22) + 19;
            %group.getObject(%j).renderInPlace(%position);
         }
      }
      else
         %position += getWord(%group.container.extent,1);
   }
   RTBCC_Roster_Swatch.reshape();
   
   if(RTBCC_Roster_Swatch.getObject(0).getClassName() $= "GuiMLTextCtrl")
      RTBCC_Roster.render();
}

//- RTBCC_RosterGroup::renderInPlace (renders a roster group taking a position argument)
function RTBCC_RosterGroup::renderInPlace(%this,%position)
{
   %extent = ((%this.getCount() + 1) * 22) + 4;
   %this.container = new GuiSwatchCtrl()
   {
      position = %position;
      extent = "194" SPC %extent;
      color = "0 0 0 0";
      
      new GuiBitmapCtrl()
      {
         position = "0 0";
         extent = "16 16";
         bitmap = $RTB::Path @ "images/icons/" @ %this.icon;
      };
      
      new GuiMLTextCtrl()
      {
         position = "20 2";
         extent = "160 12";
         text = "<color:333333><font:Verdana Bold:12>" @ %this.name;
         
         selectable = false;
      };
   };
   RTBCC_Roster_Swatch.add(%this.container);
}

//- RTBCC_RosterGroup::rerender (rerenders the roster group if it's already rendered)
function RTBCC_RosterGroup::rerender(%this)
{
   if(%this.isRendered())
      %this.unrender();
   %this.render();
}

//- RTBCC_RosterGroup::unrender (unrenders a roster group and its user items)
function RTBCC_RosterGroup::unrender(%this)
{
   if(!%this.isRendered())
      return;

   %position = getWord(%this.container.position,1);
   %extent = getWord(%this.container.extent,1);
   
   %this.container.delete();
   RTBCC_Roster_Swatch.conditionalShiftY(%position,"-"@%extent);
   RTBCC_Roster_Swatch.reshape();
   
   RTBCC_Roster.render();
}

//*********************************************************
//* Roster Interaction
//*********************************************************
//- Event_BuddyListSelect::onMouseEnter (handles entry interaction with roster user item)
function Event_BuddyListSelect::onMouseEnter(%this)
{
   %this.select.setVisible(true);
}

//- Event_BuddyListSelect::onMouseLeave (handles leaving interaction with roster user item)
function Event_BuddyListSelect::onMouseLeave(%this)
{     
   %this.select.setVisible(false);
   
   %this.user.closeRosterMenu();
}

//- Event_BuddyListSelect::onMouseDown (handles click interaction with roster user item)
function Event_BuddyListSelect::onMouseDown(%this)
{
   if(isObject(RTBCC_Roster.gui_userMenu) && RTBCC_Roster.gui_userMenu.user !$= %this.user)
      RTBCC_Roster.gui_userMenu.user.closeRosterMenu();      
      
   if(isObject(%this.user.gui_menu))
      return;
      
   %this.select.setBitmap($RTB::Path @ "images/ui/buddyListSelect_h");
}

//- Event_BuddyListSelect::onMouseUp (handles click interaction with roster user item)
function Event_BuddyListSelect::onMouseUp(%this)
{
   if(isObject(RTBCC_Roster.gui_userMenu) && RTBCC_Roster.gui_userMenu.user !$= %this.user)
      return;
      
   if(isObject(%this.user.gui_menu))
   {
      %this.user.closeRosterMenu();
      
      if((getSimTime() - %this.lastClickTime) <= 300 && %this.user.online)
         %this.user.openChatWindow();
         
      return;
   }
   %this.user.openRosterMenu();
   
   %this.lastClickTime = getSimTime();
}

//- Event_BuddyListSelect::onRightMouseDown (handles click interaction with roster user item)
function Event_BuddyListSelect::onRightMouseDown(%this)
{
   if(isObject(RTBCC_Roster.gui_userMenu) && RTBCC_Roster.gui_userMenu.user !$= %this.user)
      RTBCC_Roster.gui_userMenu.user.closeRosterMenu();      
      
   if(isObject(%this.user.gui_menu))
      return;
      
   %this.select.setBitmap($RTB::Path @ "images/ui/buddyListSelect_h");
}

//- Event_BuddyListSelect::onRightMouseUp (handles click interaction with roster user item)
function Event_BuddyListSelect::onRightMouseUp(%this)
{
   if(isObject(RTBCC_Roster.gui_userMenu) && RTBCC_Roster.gui_userMenu.user !$= %this.user)
      return;
      
   if(isObject(%this.user.gui_menu))
   {
      %this.user.closeRosterMenu();
      return;
   }
   %this.user.openRosterMenu();
   
   %this.lastClickTime = getSimTime();
}

//- Event_BuddyListMenu::onMouseEnter (handles menu item interaction of the roster interact menu)
function Event_BuddyListMenu::onMouseEnter(%this)
{
   %this.item.setBitmap($RTB::Path @ "images/ui/buddyListMenu_h");
}

//- Event_BuddyListMenu::onMouseLeave (handles menu item interaction of the roster interact menu)
function Event_BuddyListMenu::onMouseLeave(%this)
{
   %this.item.setBitmap($RTB::Path @ "images/ui/buddyListMenu_n");
}

//- Event_BuddyListMenu::onMouseUp (handles menu item interaction of the roster interact menu)
function Event_BuddyListMenu::onMouseUp(%this)
{
   eval(%this.command);
}

//*********************************************************
//* Roster User Interact Menu
//*********************************************************
//- RTBCC_RosterUser::openRosterMenu (opens a menu of items for the user to select)
function RTBCC_RosterUser::openRosterMenu(%this)
{
   %this.gui_selectBox.setBitmap($RTB::Path @ "images/ui/buddyListSelect_d");
   
   %top = getWord(%this.gui_selectBox.getPosRelativeTo(RTBCC_Roster_Scroll),1);
   %bottom = %top + getWord(%this.gui_selectBox.extent,1);
   %scrollExt = getWord(RTBCC_Roster_Scroll.extent,1)-2;
   
   if(%top < 0)
      RTBCC_Roster_Swatch.resize(1,getWord(RTBCC_Roster_Swatch.position,1)-(%top-2),194,getWord(RTBCC_Roster_Swatch.extent,1));
   if(%bottom > %scrollExt)
      RTBCC_Roster_Swatch.resize(1,getWord(RTBCC_Roster_Swatch.position,1)-(%bottom-%scrollExt)-2,194,getWord(RTBCC_Roster_Swatch.extent,1));
   
   %menuItems = -1;
   if(%this.online)
   {
      if(%this.state !$= "blocked")
      {
         %menuIcon[%menuItems++] = "comment";
         %menuText[%menuItems] = "Chat";
         %menuComm[%menuItems] = %this@".openChatWindow();";
      }
      if(%this.status $= 3 || %this.status $= 5)
      {
         %menuIcon[%menuItems++] = "world";
         %menuText[%menuItems] = "Play";
         %menuComm[%menuItems] = %this@".joinServer();";
      }
      if(RTB_ConnectClient.status $= 3 || RTB_ConnectClient.status $= 5)
      {
         %menuIcon[%menuItems++] = "house";
         %menuText[%menuItems] = "Invite";
         %menuComm[%menuItems] = %this@".inviteToServer();";
      }
   }

   %menuIcon[%menuItems++] = "information";
   %menuText[%menuItems] = "Info";
   %menuComm[%menuItems] = %this@".info();";

   if(%this.state $= "")
   {
      %menuIcon[%menuItems++] = "block";
      %menuText[%menuItems] = "Block";
      %menuComm[%menuItems] = %this@".block();";
   }
   else if(%this.state $= "blocked")
   {
      %menuIcon[%menuItems++] = "block";
      %menuText[%menuItems] = "Unblock";
      %menuComm[%menuItems] = %this@".unblock();";
   }
   if(%this.state $= "pending_to")
   {
      %menuIcon[%menuItems++] = "delete";
      %menuText[%menuItems] = "Cancel";
      %menuComm[%menuItems] = %this@".cancelSubscribe();";
      %menuItems++;
   }
   else
   {
      %menuIcon[%menuItems++] = "delete";
      %menuText[%menuItems] = "Remove";
      %menuComm[%menuItems] = %this@".unsubscribe();";
      %menuItems++;
   }
   %menuSize = (%menuItems * 20) + 4;
   
   %container = RTB_Overlay;
   %position = %this.gui_selectBox.getAbsPosition(%container);
   %menu = new GuiSwatchCtrl()
   {
      position = vectorAdd(%position,"104 22");
      extent = "66" SPC %menuSize;
      color = "0 0 0 0";
      
      user = %this;
      
      new GuiBitmapCtrl()
      {
         position = "0" SPC %menuSize - 4;
         extent = "66 4";
         
         bitmap = $RTB::Path @ "images/ui/buddyListMenuBottom";
      };
   };
   %container.add(%menu);
   %this.gui_menu = %menu;
   RTBCC_Roster.gui_userMenu = %menu;
   
   for(%i=0;%i<%menuItems;%i++)
   {
      %item = new GuiBitmapCtrl()
      {
         position = "0" SPC (%i * 20);
         extent = "66 20";
         
         bitmap = $RTB::Path @ "images/ui/buddyListMenu_n";
         
         new GuiBitmapCtrl()
         {
            position = "4 2";
            extent = "16 16";
            
            bitmap = $RTB::Path @ "images/icons/" @ %menuIcon[%i];
         };
         
         new GuiMLTextCtrl()
         {
            position = "22 4";
            extent = "54 12";
            
            text = "<color:444444><font:Verdana:12>" @ %menuText[%i];
            
            selectable = false;
         };
      };
      %menu.add(%item);
      
      %mouseEvent = new GuiMouseEventCtrl()
      {
         position = "0" SPC (%i * 20);
         extent = "66 20";
         
         eventType = "BuddyListMenu";
         eventCallbacks = "1101000";
         
         user = %this;
         item = %item;
         command = %menuComm[%i];
      };
      %menu.add(%mouseEvent);
   }
   %this.gui_mouseEvent.extent = "170" SPC (%menuSize + 30);
   
   if(RTBCO_getPref("CC::EnableSounds"))
      alxPlay(RTBCC_TickSound);
}

//- RTBCC_RosterUser::closeRosterMenu (closes the roster menu)
function RTBCC_RosterUser::closeRosterMenu(%this)
{
   if(isObject(%this.gui_menu))
   {
      RTBCC_Roster.gui_userMenu = "";
      %this.gui_menu.schedule(1,"delete");
      if(RTBCO_getPref("CC::EnableSounds"))
         alxPlay(RTBCC_TickSound);
   }
   %this.gui_mouseEvent.extent = "170 22";
   %this.gui_selectBox.setBitmap($RTB::Path @ "images/ui/buddyListSelect_n");
}

//- RTBCC_RosterUser::openChatWindow (opens a chat window with the user)
function RTBCC_RosterUser::openChatWindow(%this)
{
   %this.closeRosterMenu();
   
   if(!RTBCC_SessionManager.hasID(%this.id))
      RTBCC_SessionManager.createSession(%this.id);
   else
      RTBCC_SessionManager.getByID(%this.id).render();
}

//- RTBCC_RosterUser::joinServer (joins the user's server)
function RTBCC_RosterUser::joinServer(%this,%skip)
{
   %this.closeRosterMenu();
   
   %ip = %this.server["ip"];
   %port = %this.server["port"];
   
   if(isObject(ServerConnection) && !%skip)
   {
      if(ServerConnection.getAddress() $= "local")
         RTB_ConnectClient.messageBoxYesNo("Are you sure?","You're currently hosting a server - are you sure you want to close it and join this game?",%this@".joinServer(1);","");
      else
         RTB_ConnectClient.messageBoxYesNo("Are you sure?","Are you sure you want to leave your current server and join this game?",%this@".joinServer(1);","");
      return;
   }
   
   $RTB::MCCC::Cache::Joining = %ip@":"@%port;
   RTB_ConnectClient.messageBox("Joining ...","Getting server data ...");
   RTBCC_Socket.getServerStatus(%ip,%port);
}

//- RTBCC_RosterUser::inviteToServer (invites user to your server)
function RTBCC_RosterUser::inviteToServer(%this)
{
   %this.closeRosterMenu();
   
   RTB_ConnectClient.messageBox("Inviting ...","Sending an invitation to "@%this.name@" ...");
   RTBCC_Socket.sendServerInvite(%this.id);
}

//- RTBCC_RosterUser::info (shows information on the user)
function RTBCC_RosterUser::info(%this)
{
   %this.closeRosterMenu();
   
   RTBCC_Socket.getUserInfo(%this.id);
   RTB_ConnectClient.messageBox("Please Wait ...","Getting user details for Blockland ID "@%this.id);
}

//- RTBCC_RosterUser::block (blocks user from contacting us)
function RTBCC_RosterUser::block(%this,%confirm)
{
   %this.closeRosterMenu();
   
   if(!%confirm)
      RTB_ConnectClient.messageBoxYesNo("Really?","Are you sure you want to block "@%this.name@" from messaging you?",%this@".block(1);");
   else
      RTBCC_Socket.blockUser(%this.id);
}

//- RTBCC_RosterUser::unblock (allows blocked user to contact us)
function RTBCC_RosterUser::unblock(%this)
{
   %this.closeRosterMenu();
   
   RTBCC_Socket.unblockUser(%this.id);
}

//- RTBCC_RosterUser::unsubscribe (menu action to remove user from roster)
function RTBCC_RosterUser::unsubscribe(%this,%confirm)
{
   %this.closeRosterMenu();
   
   if(!%confirm)
      RTB_ConnectClient.messageBoxYesNo("For Serious?","Are you sure you want to remove "@%this.name@" from your friends list?",%this@".unsubscribe(1);");
   else
      RTBCC_Socket.removeFromRoster(%this.id);
}

//- RTBCC_RosterUser::cancelSubscribe (menu action to remove user from roster)
function RTBCC_RosterUser::cancelSubscribe(%this,%confirm)
{
   %this.closeRosterMenu();
   
   if(!%confirm)
      RTB_ConnectClient.messageBoxYesNo("Are you sure?","Do you really want to cancel this friend invitation?",%this@".cancelSubscribe(1);");
   else
      RTBCC_Socket.removeFromRoster(%this.id);
}

//*********************************************************
//* Invite Roster Live Rendering & Manipulation
//*********************************************************
//- RTBCC_InviteRoster (renders an indicator of number of invites)
function RTBCC_InviteRoster::render(%this)
{
   if(%this.getCount() <= 0)
   {
      if(isObject(RTBCC_InviteButton))
      {
         RTBCC_InviteButton.delete();
         RTBCC_Roster_Scroll.resize(3,3,196,getWord(RTBCC_Roster_Scroll.extent,1)+28);
      }
   }
   else
   {
      if(isObject(RTBCC_InviteButton))
         RTBCC_InviteButton.delete();
      else
         RTBCC_Roster_Scroll.resize(3,3,196,getWord(RTBCC_Roster_Scroll.extent,1)-28);
         
      if(%this.getCount() > 1)
         %text = "You have "@%this.getCount()@" friend invitations.";
      else
         %text = "You have 1 friend invitation.";
         
      %button = new GuiBitmapButtonCtrl(RTBCC_InviteButton)
      {
         profile = RTB_TextEditProfile;
         vertSizing = "top";
         position = "4" SPC getWord(RTBCC_Window_Roster.extent,1)-54;
         extent = "194 24";
         bitmap = $RTB::Path@"images/ui/buttons/connectClient/infoBar";
         text = "        "@%text;
         command = "RTBCC_InviteRoster.open();";
      };
      RTBCC_Window_Roster.add(%button);
   }
}

//- RTBCC_InviteRoster::unrender (unrenders the invite roster indicator)
function RTBCC_InviteRoster::unrender(%this)
{
   if(isObject(RTBCC_InviteButton))
   {
      RTBCC_InviteButton.delete();
      RTBCC_Roster_Scroll.resize(3,29,196,getWord(RTBCC_Roster_Scroll.extent,1)+28);
   }
}

//*********************************************************
//* Invite Roster Interaction
//*********************************************************
//- RTBCC_InviteRoster::open (opens the invite approve/deny)
function RTBCC_InviteRoster::open(%this)
{
   if(%this.getCount() $= 0)
   {
      %this.render();
      return;
   }
   
   if(%this.getCount() $= 1)
   {
      %invite = %this.getObject(0);
      RTB_ConnectClient.messageBoxYesNo("Friend Request",%invite.name@" ("@%invite.id@") has requested to be friends with you. Would you like to accept?","RTBCC_InviteRoster.accept("@%invite@");","RTBCC_InviteRoster.reject("@%invite@");");
      return;
   }
   
   RTB_ConnectClient.setModalWindow("Invites");
   RTBCC_Invites_Swatch.resize(0,0,154,144);
   RTBCC_Invites_Swatch.clear();
   
   %nextY = 0;
   for(%i=0;%i<%this.getCount();%i++)
   {
      %invite = %this.getObject(%i);
      %mlText = new GuiMLTextCtrl()
      {
         position = "2" SPC %nextY;
         extent = "123 24";
         text = "<color:444444><font:Verdana Bold:12>"@%invite.name@"<br><font:Arial:12>Blockland ID "@%invite.id;
         
         selectable = false;
      };
      RTBCC_Invites_Swatch.add(%mlText);
      
      %checkboxA = new GuiCheckboxCtrl()
      {
         profile = RTB_CheckboxProfile;
         position = "108" SPC %nextY;
         extent = "17 24";
         text = " ";
         buttonType = "ToggleButton";
         
         id = %invite.id;
         type = "yes";
      };
      RTBCC_Invites_Swatch.add(%checkboxA);
      
      %checkboxB = new GuiCheckboxCtrl()
      {
         profile = RTB_CheckboxProfile;
         position = "128" SPC %nextY;
         extent = "17 24";
         text = " ";
         buttonType = "ToggleButton";
         
         id = %invite.id;
         type = "no";
      };
      RTBCC_Invites_Swatch.add(%checkboxB);
      
      %checkboxA.command = %checkboxB@".setValue(0);";
      %checkboxB.command = %checkboxA@".setValue(0);";
      
      if(%i < %this.getCount() - 1)
      {
         %divider = new GuiSwatchCtrl()
         {
            position = "0" SPC %nextY+27;
            extent = "145 1";
            color = "200 200 200 255";
            minExtent = "1 1";
         };
         RTBCC_Invites_Swatch.add(%divider);
      }
      %nextY += 30;
   }
   RTBCC_Invites_Swatch.resize(0,0,154,%nextY);
}

//- RTBCC_InviteRoster::done (accepts/denies all the invites)
function RTBCC_InviteRoster::done(%this)
{
   %accepted = 0;
   %rejected = 0;
   
   for(%i=0;%i<RTBCC_Invites_Swatch.getCount();%i++)
   {
      %ctrl = RTBCC_Invites_Swatch.getObject(%i);
      if(%ctrl.getClassName() $= "GuiCheckboxCtrl")
      {
         %invite = RTBCC_InviteRoster.getByID(%ctrl.id);
         if(%ctrl.type $= "yes" && %ctrl.getValue() $= 1)
         {
            RTBCC_InviteRoster.accept(%invite);
            %accepted++;
         }
         else if(%ctrl.type $= "no" && %ctrl.getValue() $= 1)
         {
            RTBCC_InviteRoster.reject(%invite,0);
            %rejected++;
         }
         %total++;
      }
   }
   %total /= 2;
   
   if(%accepted $= %total)
      RTB_ConnectClient.messageBoxOK("Nice One!","You accepted all "@%total@" invites.");
   else if(%rejected $= %total)
      RTB_ConnectClient.messageBoxOK("Nice One!","You rejected all "@%total@" invites.");
   else if (%rejected $= 0 && %accepted $= 0)
      RTB_ConnectClient.messageBoxOK("Nice One!","You have done ... absolutely nothing?");
   else if (%rejected > %accepted)
      RTB_ConnectClient.messageBoxOK("Nice One!","You have rejected "@%rejected@" of your "@%total@" invites.");
   else
      RTB_ConnectClient.messageBoxOK("Nice One!","You have accepted "@%accepted@" of your "@%total@" invites.");
}

//- RTBCC_InviteRoster::close (closes the invite modal window)
function RTBCC_InviteRoster::close(%this)
{
   RTB_ConnectClient.closeModalWindow();
}

//*********************************************************
//* Loading Window Functionality
//*********************************************************
//- RTBCC_Roster_Loading::setStage (sets the stage of loading which decides message/fps)
function RTBCC_Roster_Loading::setStage(%this,%stage)
{
   if(%stage $= 1)
   {
      %fps = 15;
      %text = "Logging In";
   }
   else if(%stage $= 2)
   {
      %fps = 30;
      %text = "Loading Roster";
   }
   else
   {
      %fps = 5;
      %text = "Connecting";
   }
   RTBCC_Loading_Ring.setVisible(true);
   RTBCC_Loading_RingFail.setVisible(false);
   RTBCC_Loading_Text.setText("<color:444444><font:Verdana:12><just:center>" @ %text);
   RTBCC_Loading_Ring.framesPerSecond = %fps;
}

//- RTBCC_Roster_Loading::error (displays an error on the loading page)
function RTBCC_Roster_Loading::error(%this,%error)
{
   RTBCC_Roster_Loading.setVisible(true);
   RTBCC_Loading_Ring.setVisible(false);
   RTBCC_Loading_RingFail.setVisible(true);
   RTBCC_Loading_Text.setText("<color:FF0000><font:Verdana Bold:12><just:center>" @ %error);
   if(RTB_ConnectClient.isOpen())
      RTBCC_Loading_Text.forceReflow();
}

//*********************************************************
//* Session Manager Implementation
//*********************************************************
//- RTBCC_createSessionManager (creates a chat session manager)
function RTBCC_createSessionManager()
{
   if(isObject(RTBCC_SessionManager))
   {
      RTBCC_SessionManager.destroy();
      RTBCC_SessionManager.delete();
   }
   
   %manager = new ScriptGroup(RTBCC_SessionManager);
   RTBGroup.add(%manager);
   
   return %manager;
}

//- RTBCC_SessionManager::resetCursor (resets cursor to active chat or best alternative)
function RTBCC_SessionManager::resetCursor(%this)
{
   if(isObject(%this.lastFocus) && %this.isMember(%this.lastFocus))
      %this.lastFocus.focus();
   else if(%this.getCount() > 0)
   {
      for(%i=%this.getCount()-1;%i>=0;%i--)
      {
         %session = %this.getObject(%i);
         if(%session.isRendered())
         {
            %session.focus();
            break;
         }
      }
   }
}

//- RTBCC_SessionManager::createSession (creates a chat session for a user)
function RTBCC_SessionManager::createSession(%this,%id)
{
   if(%this.hasID(%id))
      return %this.getByID(%id);
      
   if(!RTBCC_Roster.hasID(%id))
      %user = RTBCC_TempRoster.getByID(%id);
   else
      %user = RTBCC_Roster.getByID(%id);
      
   %session = new ScriptObject()
   {
      class = "RTBCC_Session";
      
      id = %id;
      user = %user;
   };
   %this.add(%session);
   
   %session.render();
   
   return %session;
}

//- RTBCC_SessionManager::hasID (checks to see if we have a session for this id)
function RTBCC_SessionManager::hasID(%this,%id)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      if(%this.getObject(%i).id $= %id)
         return true;
   }
   return false;
}

//- RTBCC_SessionManager::getByID (returns session by id)
function RTBCC_SessionManager::getByID(%this,%id)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %session = %this.getObject(%i);
      if(%session.id $= %id)
         return %session;
   }
   return false;
}

//- RTBCC_SessionManager::reclaim (resets user ids within sessions)
function RTBCC_SessionManager::reclaim(%this)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %session = %this.getObject(%i);
      if(RTBCC_Roster.hasID(%session.id))
         %session.user = RTBCC_Roster.getByID(%session.id);
      else
         %session.user = RTBCC_TempRoster.getByID(%session.id);
   }
}

//- RTBCC_SessionManager::destroy (destroys sessions and their renderings)
function RTBCC_SessionManager::destroy(%this)
{
   while(%this.getCount() >= 1)
   {
      %session = %this.getObject(0);
      %session.unrender();
      %session.delete();
   }
}

//*********************************************************
//* Chat Session Implementation
//*********************************************************
//- RTBCC_Session::send (sends whatever is in the text box)
function RTBCC_Session::send(%this)
{
   %text = %this.window.input.getValue();
   if(%text $= "")
      return;
      
   if(!RTBCC_Socket.authenticated)
   {
      %this.writeNotice("You're not currently signed in!");
      return;
   }
      
   %this.window.input.setValue("");
   
   %this.stoppedTyping();
   if(getSubStr(%text,0,1) $= "/")
   {
      if(firstWord(%text) $= "/me" || firstWord(%text) $= "/action")
      {
         %this.writeAction(RTB_ConnectClient.client_name,parseLinks(stripMLControlChars(restWords(%text))));
         RTBCC_Socket.sendMessage(%this.id,restWords(%text),true);
      }
   }
   else
   {
      %this.writeMessage(RTB_ConnectClient.client_name,parseLinks(stripMLControlChars(%text)));
      RTBCC_Socket.sendMessage(%this.id,%text);
   }
   %this.focus();
   
   %this.window.scroll.scrollToBottom();
}

//- RTBCC_Session::receive (handles a message packet)
function RTBCC_Session::receive(%this,%message)
{
   if(!%this.isRendered())
      %this.render();

   if(%message.attrib["type"] $= "action")
      %this.writeAction(%this.user.name,%message.find("body").cData);
   else
      %this.writeMessage(%this.user.name,%message.find("body").cData);
   
   if(!RTB_Overlay.isAwake())
   {
      if(RTBCO_getPref("CC::Message::Beep"))
         alxPlay(RTBCC_MessageSound);
      if(RTBCO_getPref("CC::Message::Note"))
         RTBCC_NotificationManager.push(%this.user.name,"has just sent you a message.","comments",%this.user.id@"_msg",-1);

      %this.focus();
   }
   %this.lastMessage = getDateTime();
   %this.updateStatus(0);
}

//- RTBCC_Session::handleInvite (handles an invite)
function RTBCC_Session::handleInvite(%this,%ip,%port)
{
   if(!%this.isRendered())
      %this.render();
      
   %this.writeInfo(%this.user.name @ " has invited you to play with them.");
   %this.setInviteDisplay(1);
   
   %this.lastMessage = getDateTime();
}

//- RTBCC_Session::writeMessage (adds a user-sent message to the bottom of the ml text)
function RTBCC_Session::writeMessage(%this,%sender,%message)
{
   if(RTBCO_getPref("CC::ChatLogging"))
      %this.log(%sender@": "@%message);
      
   %message = "<font:Verdana Bold:12>"@%sender@"<font:Verdana:12>: "@%message;
   
   if(RTBCO_getPref("CC::ShowTimestamps"))
      %message = "<font:Verdana Bold:12>["@getSubStr(getWord(getDateTime(),1),0,8)@"] " @ %message;
      
   %this.write(%message);
}

//- RTBCC_Session::writeAction (adds an action message to the bottom of the ml text)
function RTBCC_Session::writeAction(%this,%sender,%message)
{
   if(RTBCO_getPref("CC::ChatLogging"))
      %this.log("* "@%sender@" "@%message);
      
   %message = "<font:Verdana Bold:12><color:CC00CC>* "@%sender@" <font:Verdana:12>"@%message@"<color:444444>";
   
   if(RTBCO_getPref("CC::ShowTimestamps"))
      %message = "<font:Verdana Bold:12>["@getSubStr(getWord(getDateTime(),1),0,8)@"] " @ %message;
   
   %this.write(%message);
}

//- RTBCC_Session::writeNotice (adds a notice message to the bottom of the ml text)
function RTBCC_Session::writeNotice(%this,%message)
{
   if(RTBCO_getPref("CC::ChatLogging"))
      %this.log("* "@%message);
      
   %message = "<font:Verdana:12><color:666666>* "@%message@"<color:444444>";
   
   if(RTBCO_getPref("CC::ShowTimestamps"))
      %message = "<font:Verdana Bold:12>["@getSubStr(getWord(getDateTime(),1),0,8)@"] " @ %message;
   
   %this.write(%message);
}

//- RTBCC_Session::writeInfo (adds an information message to the bottom of the ml text)
function RTBCC_Session::writeInfo(%this,%message)
{
   if(RTBCO_getPref("CC::ChatLogging"))
      %this.log("* "@%message);
      
   %message = "<font:Verdana:12><color:00AA00>* "@%message@"<color:444444>";
   
   if(RTBCO_getPref("CC::ShowTimestamps"))
      %message = "<font:Verdana Bold:12>["@getSubStr(getWord(getDateTime(),1),0,8)@"] " @ %message;
   
   %this.write(%message);
}

//- RTBCC_Session::writeError (adds an error message to the bottom of the ml text)
function RTBCC_Session::writeError(%this,%message)
{
   %message = "<color:FF0000>* "@%message@"<color:444444>";
   
   %this.write(%message);
}

//- RTBCC_Session::write (adds a line to the bottom of the ml text)
function RTBCC_Session::write(%this,%line)
{
   if(!%this.isRendered())
      return;
      
   %scroll = %this.window.scroll;
   %display = %this.window.display;
   
   %position = getWord(%display.position,1);
   if((getWord(%display.extent,1)+getWord(%display.position,1)) <= (getWord(%scroll.extent,1)-1))
      %atBottom = true;
   
   if(%display.getValue() $= "")
      %display.setValue(%line);
   else
      %display.setValue(%display.getValue()@"\n"@%line);
      
   if(RTB_Overlay.isAwake())
      %display.forceReflow();
      
   %display.setCursorPosition(strLen(%display.getValue()));
   
   if(%atBottom)
      %scroll.scrollToBottom();
   else
      %display.resize(getWord(%display.position,0),%position,getWord(%display.extent,0),getWord(%display.extent,1));
}

//- RTBCC_Session::log (logs a message into the chat logging)
function RTBCC_Session::log(%this,%message)
{
   if(!isObject(%this.logger))
   {
      %this.logger = new FileObject();
      %this.logger.openForAppend("config/client/rtb/logs/users/"@%this.id@".txt");
      %this.logger.writeLine("");
      %this.logger.writeLine(getDateTime());
      %this.logger.writeLine("--");
      %this.logger.close();
   }
   %this.logger.openForAppend("config/client/rtb/logs/users/"@%this.id@".txt");
   %this.logger.writeLine("["@lastWord(getDateTime())@"] "@stripMLControlChars(%message));
   %this.logger.close();
   
   RTBGroup.add(%this.logger);
}

//- RTBCC_Session::updateStatus (updates typing status messages)
function RTBCC_Session::updateStatus(%this,%status)
{
   %this.typingStatus = %status;
   
   if(!%this.isRendered())
      return;
   
   if(%this.typingStatus $= 1)
      %this.window.status.setValue("<color:888888><font:Verdana:12>"@%this.user.name@" is typing a message ...");
   else if(%this.typingStatus $= 2)
      %this.window.status.setValue("<color:888888><font:Verdana:12>"@%this.user.name@" has entered a message.");
   else
      if(%this.lastMessage !$= "" && !RTBCO_getPref("CC::ShowTimestamps"))
         %this.window.status.setValue("<color:888888><font:Verdana:12>Last message received on "@getWord(%this.lastMessage,0)@" at "@getWord(%this.lastMessage,1)@".");
      else
         %this.window.status.setValue("");
}

//- RTBCC_Session::typing (indicates to server that user is typing)
function RTBCC_Session::typing(%this)
{
   if(isEventPending(%this.typingSchedule))
      cancel(%this.typingSchedule);
      
   if(!%this.typingTo)
      RTBCC_Socket.sendTypingStatus(%this.id,1);
      
   %this.focus();
      
   RTBCC_SessionManager.lastFocus = %this;
   RTBCC_SessionManager.lastFocusTime = getSimTime();
   
   %this.typingTo = true;
   %this.typingSchedule = %this.schedule(3000,"stoppedTyping");
}

//- RTBCC_Session::stoppedTyping (notifies server that user is not typing)
function RTBCC_Session::stoppedTyping(%this)
{
   if(isEventPending(%this.typingSchedule))
      cancel(%this.typingSchedule);
         
   %this.typingTo = false;
   
   if(isObject(%this.window) && %this.window.input.getValue() !$= "")
      RTBCC_Socket.sendTypingStatus(%this.id,2);
   else
      RTBCC_Socket.sendTypingStatus(%this.id,0);
}

//- RTBCC_Session::focus (brings the window into user focus)
function RTBCC_Session::focus(%this)
{
   if(%this.isRendered())
   {
      RTB_Overlay.pushToBack(%this.window);
      %this.window.input.makeFirstResponder(1);
   }
}

//- RTBCC_Session::render (renders a chat window for the session)
function RTBCC_Session::render(%this)
{
   if(%this.isRendered())
   {
      %this.focus();
      return;
   }
      
   %window = new GuiWindowCtrl()
   {
      profile = GuiWindowProfile;
      position = "0 0";
      extent = "300 202";
      minExtent = "300 202";
      text = %this.user.name;
      resizeWidth = true;
      resizeHeight = true;
      canMove = true;
      canClose = true;
      canMinimize = false;
      canMaximize = false;
      
      new GuiBitmapBorderCtrl()
      {
         profile = RTB_ContentBorderProfile;
         horizSizing = "width";
         vertSizing = "height";
         position = "7 30";
         extent = "287 140";
         
         new GuiSwatchCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "width";
            vertSizing = "height";
            position = "3 3";
            extent = "281 134";
            color = "255 255 255 255";

            new GuiScrollCtrl()
            {            
               profile = RTB_ScrollProfile;
               horizSizing = "width";
               vertSizing = "height";
               position = "1 1";
               extent = "279 119";
               hScrollBar = "alwaysOff";
                  
               new GuiMLTextCtrl()
               {
                  profile = RTB_MLEditProfile;
                  horizSizing = "width";
                  vertSizing = "height";
                  position = "1 1";
                  extent = "265 84";
               };
            };
            new GuiMLTextCtrl()
            {
               profile = GuiDefaultProfile;
               horizSizing = "right";
               vertSizing = "top";
               position = "2 120";
               extent = "280 14";
               
               selectable = false;
            };
         };
      };
      
      new GuiBitmapBorderCtrl()
      {
         profile = RTB_ContentBorderProfile;
         horizSizing = "width";
         vertSizing = "top";
         position = "7 173";
         extent = "262 22";
         
         new GuiSwatchCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "width";
            vertSizing = "height";
            position = "3 3";
            extent = "256 16";
            color = "255 255 255 255";
         };
         
         new GuiBitmapCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "left";
            vertSizing = "bottom";
            position = "243 2";
            extent = "16 16";
            bitmap = $RTB::Path@"images/icons/bullet_go";
         };
         
         new GuiBitmapButtonCtrl() {
            profile = "GuiDefaultProfile";
            horizSizing = "left";
            vertSizing = "bottom";
            position = "243 2";
            extent = "16 16";
            command = %this@".send();";
            text = " ";
         };
      };
      
      new GuiBitmapBorderCtrl()
      {
         profile = RTB_ContentBorderProfile;
         horizSizing = "left";
         vertSizing = "top";
         position = "272 173";
         extent = "22 22";
         
         new GuiSwatchCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "width";
            vertSizing = "height";
            position = "3 3";
            extent = "16 16";
            color = "255 255 255 255";
         };
         
         new GuiBitmapCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "left";
            vertSizing = "bottom";
            position = "3 3";
            extent = "16 16";
            bitmap = $RTB::Path@"images/icons/delete";
         };
         
         new GuiBitmapButtonCtrl() {
            profile = "GuiDefaultProfile";
            horizSizing = "left";
            vertSizing = "bottom";
            position = "2 2";
            extent = "18 18";
            command = %this@".block();";
            text = " ";
         };
      };
      
      new GuiSwatchCtrl()
      {
         profile = GuiDefaultProfile;
         horizSizing = "width";
         vertSizing = "bottom";
         position = "7 30";
         extent = "287 25";
         color = "0 0 0 0";
         visible = 0;
         
         new GuiBitmapBorderCtrl()
         {
            profile = RTB_ContentBorderProfile;
            horizSizing = "width";
            vertSizing = "top";
            position = "0 0";
            extent = "287 25";
            
            new GuiSwatchCtrl()
            {
               profile = GuiDefaultProfile;
               horizSizing = "width";
               vertSizing = "height";
               position = "3 3";
               extent = "281 19";
               color = "255 255 255 255";
            };
         };
         new GuiSwatchCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "width";
            vertSizing = "height";
            position = "1 1";
            extent = "285 23";
            color = "100 200 100 100";
         };
         new GuiBitmapCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "right";
            vertSizing = "bottom";
            position = "6 4";
            extent = "16 16";
            bitmap = $RTB::Path@"images/icons/email_go";
         };
         new GuiMLTextCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "right";
            vertSizing = "top";
            position = "28 6";
            extent = "250 12";
            text = "<font:Verdana:12><color:444444>"@%this.user.name@" has invited you to play.";
            selectable = false;
         };     
         new GuiBitmapButtonCtrl() {
            profile = "GuiDefaultProfile";
            horizSizing = "left";
            vertSizing = "bottom";
            position = "0 0";
            extent = "260 25";
            command = %this@".user.joinServer();"@%this@".setInviteDisplay(0);";
            text = " ";
         };
         new GuiBitmapButtonCtrl() {
            profile = "GuiDefaultProfile";
            horizSizing = "left";
            vertSizing = "bottom";
            position = "269 8";
            extent = "9 9";
            command = %this@".setInviteDisplay(0);";
            text = " ";
            bitmap = $RTB::Path@"images/ui/buttons/connectClient/closeInvite";
         };
      };
   };
   RTB_Overlay.add(%window);
 
   %input = RTBCC_InputRecycler.get();
   %window.getObject(1).add(%input);
   %input.horizSizing = "width";
   %input.vertSizing = "bottom";
   %input.position = "1 3";
   %input.extent = "240 16";
   %input.command = %this@".typing();";
   %input.altCommand = %this@".send();";
 
   %window.scrollContainer = %window.getObject(0);
   %window.scroll = %window.scrollContainer.getObject(0).getObject(0);
   %window.status = %window.scrollContainer.getObject(0).getObject(1);
   %window.display = %window.scroll.getObject(0);
   %window.input = %input;
   %window.blockBtn = %window.getObject(2);
   %window.inviteBanner = %window.getObject(3);
   %window.closeCommand = %this@".unrender();";
   
   if(%this.user.state $= "blocked")
   {
      %window.blockBtn.getObject(1).setBitmap($RTB::Path@"images/icons/unblock");
      %window.blockBtn.getObject(2).command = %this@".unblock();";
   }
   else
   {
      %window.blockBtn.getObject(1).setBitmap($RTB::Path@"images/icons/delete");
      %window.blockBtn.getObject(2).command = %this@".block();";
   }
   
   %window.session = %this;
   %this.window = %window;
   
   %this.positionWindow();
   
   if(%this.user.conversationHistory !$= "")
   {
      %history = strReplace(strReplace(%this.user.conversationHistory,"\n","<br>"),"<color:","\t");
      %this.user.conversationHistory = "";

      %text = getField(%history,0);
      for(%i=1;%i<getFieldCount(%history);%i++)
      {
         %part = getField(%history,%i);
         %text = %text @ "<color:CCCCCC>" @ getSubStr(%part,7,strLen(%part));
      }
      %window.display.setText("<color:CCCCCC>"@%text@"<color:444444>");
      
      %window.display.setCursorPosition(strLen(%window.display.getValue()));
      %window.scroll.scrollToBottom();
   }
}

//- RTBCC_Session::positionWindow (determines best position to render chat window)
function RTBCC_Session::positionWindow(%this)
{
   %offset = 0;
   %position = "0 0";
   while(%free !$= true)
   {
      %free = true;
      for(%i=0;%i<RTBCC_SessionManager.getCount();%i++)
      {
         %session = RTBCC_SessionManager.getObject(%i);
         if(!%session.isRendered() || %session $= %this)
            continue;

         if(%session.window.position $= %position)
         {
            %free = false;
            break;
         }
      }
      
      if(%free !$= true)
      {
         %offset += 40;
         %position = %offset SPC %offset;
      }
   }
   %this.window.position = %position;
}

//- RTBCC_Session::isRendered (determines whether the session chat is rendered)
function RTBCC_Session::isRendered(%this)
{
   if(isObject(%this.window) && %this.window.session $= %this)
      return true;
   return false;
}

//- RTBCC_Session::unrender (unrenders the session chat window)
function RTBCC_Session::unrender(%this)
{
   if(!%this.isRendered())
      return;
      
   if(%this.window.input.getValue() !$= "")
      RTBCC_Socket.sendTypingStatus(%this.id,0);
      
   %this.user.conversationHistory = %this.window.display.getValue();
   
   RTBCC_InputRecycler.reclaim(%this.window.input);
   %this.window.delete();
   %this.window = "";
}

//- RTBCC_Session::block (blocks the user you're talking to)
function RTBCC_Session::block(%this)
{
   RTBCC_Socket.blockUser(%this.user.id);
}

//- RTBCC_Session::unblock (unblocks the user you're talking to)
function RTBCC_Session::unblock(%this)
{
   RTBCC_Socket.unblockUser(%this.user.id);
}

//- RTBCC_Session::setBlockedStatus (switches block buttons)
function RTBCC_Session::setBlockedStatus(%this,%status)
{
   if(%status $= 1)
   {
      %this.window.blockBtn.getObject(1).setBitmap($RTB::Path@"images/icons/unblock");
      %this.window.blockBtn.getObject(2).command = %this@".unblock();";
   }
   else
   {
      %this.window.blockBtn.getObject(1).setBitmap($RTB::Path@"images/icons/delete");
      %this.window.blockBtn.getObject(2).command = %this@".block();";
   }
}

//- RTBCC_Session::setInviteDisplay (shows or hides the invitation banner)
function RTBCC_Session::setInviteDisplay(%this,%display)
{
   if(!%this.isRendered())
      return;
      
   if(%display)
   {
      if(%this.window.inviteBanner.isVisible())
         return;
         
      %this.window.scrollContainer.resize(7,58,getWord(%this.window.scrollContainer.extent,0),getWord(%this.window.scrollContainer.extent,1)-28);
      %this.window.inviteBanner.setVisible(true);
   }
   else
   {
      if(!%this.window.inviteBanner.isVisible())
         return;
         
      %this.window.scrollContainer.resize(7,30,getWord(%this.window.scrollContainer.extent,0),getWord(%this.window.scrollContainer.extent,1)+28);
      %this.window.inviteBanner.setVisible(false);
   }
}

//*********************************************************
//* Room Manager Implementation
//*********************************************************
//- RTBCC_createRoomManager (creates a room manager)
function RTBCC_createRoomManager()
{
   if(isObject(RTBCC_RoomManager))
      RTBCC_RoomManager.delete();
      
   %manager = new ScriptGroup(RTBCC_RoomManager);
   RTBGroup.add(%manager);
   
   %manager.addGroup("Public Rooms",1,"world");
   %manager.addGroup("Player Rooms",2,"heart");
   %manager.addGroup("Other Rooms",0,"world");
   
   return %manager;
}

//- RTBCC_RoomManager::getRooms (obtain a list of rooms from the server)
function RTBCC_RoomManager::getRooms(%this)
{
   RTBCC_Socket.getRoomList();
}

//- RTBCC_RoomManager::updateRooms (updates room list)
function RTBCC_RoomManager::updateRooms(%this)
{
   RTBCC_Socket.getRoomList(1);
}

//- RTBCC_RoomManager::refresh (refreshes room details every 10 seconds)
function RTBCC_RoomManager::refresh(%this)
{
   if(isEventPending(%this.refreshSchedule))
      cancel(%this.refreshSchedule);
      
   %this.updateRooms();
   %this.refreshSchedule = %this.schedule(10000,"refresh");
}

//- RTBCC_RoomManager::stopRefresh (stops room refresh)
function RTBCC_RoomManager::stopRefresh(%this)
{
   if(isEventPending(%this.refreshSchedule))
      cancel(%this.refreshSchedule);
}

//- RTBCC_RoomManager::performAutoJoins (auto joins rooms)
function RTBCC_RoomManager::performAutoJoins(%this)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      for(%j=0;%j<%group.getCount();%j++)
      {
         %room = %group.getObject(%j);
         if(RTBCC_RoomOptionsManager.hasRoomStore(%room.name))
         {
            %store = RTBCC_RoomOptionsManager.getRoomStore(%room.name);
            if(%store.autojoin)
               %room.join();
         }
      }
   }
}

//- RTBCC_RoomManager::addRoom (creates new room object)
function RTBCC_RoomManager::addRoom(%this,%name,%icon,%type,%owner,%users,%update)
{
   %group = %this.getGroupByType(%type);
   if(%group.hasRoom(%name))
   {
      %room = %group.getRoomByName(%name);
      %room.users = %users;
      %room.render();
      return %room;
   }
   
   if(%update)
      return false;
      
   %room = %group.addRoom(%name,%icon,%owner,%users);
   
   return %room;
}

//- RTBCC_RoomManager::hasRoom (checks for a room)
function RTBCC_RoomManager::hasRoom(%this,%name)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      if(%group.hasRoom(%name))
         return true;
   }
   return false;
}

//- RTBCC_RoomManager::getRoomByName (returns a room by name)
function RTBCC_RoomManager::getRoomByName(%this,%name)
{
   if(!%this.hasRoom(%name))
      return false;
      
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      if(%group.hasRoom(%name))
         return %group.getRoomByName(%name);
   }
   return false;
}

//- RTBCC_RoomManager::addGroup (adds a new room group)
function RTBCC_RoomManager::addGroup(%this,%name,%type,%icon)
{
   if(%this.hasGroup(%name))
      return %this.getGroupByName(%name);
      
   %group = new ScriptGroup()
   {
      class = "RTBCC_RoomGroup";
      
      name = %name;
      type = %type;
      icon = "folder_user";
   };
   %this.add(%group);
   
   if(%icon !$= "")
      %group.icon = %icon;
      
   return %group;
}

//- RTBCC_RoomManager::hasGroup (checks if a room group exists)
function RTBCC_RoomManager::hasGroup(%this,%name)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      if(%this.getObject(%i).name $= %name)
         return true;
   }
   return false;
}

//- RTBCC_RoomManager::getGroupByName (returns a room group by name)
function RTBCC_RoomManager::getGroupByName(%this,%name)
{
   if(!%this.hasGroup(%name))
      return false;
      
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      if(%group.name $= %name)
         return %group;
   }
   return false;
}

//- RTBCC_RoomManager::getGroupByType (returns a room group by type)
function RTBCC_RoomManager::getGroupByType(%this,%type)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      if(%group.type $= %type)
         return %group;
   }
   return false;
}

//*********************************************************
//* Room Group Implementation
//*********************************************************
//- RTBCC_RoomGroup::addRoom (adds a room to the group)
function RTBCC_RoomGroup::addRoom(%this,%name,%icon,%owner,%users)
{
   if(%this.hasRoom(%name))
      return %this.getRoomByName(%name);
      
   %room = new ScriptGroup()
   {
      class = "RTBCC_Room";
      type = %this.name;
      
      name = %name;
      type = %this.type;
      icon = %icon;
      owner = %owner;
      users = %users;
   };
   %this.add(%room);

   return %room;
}

//- RTBCC_RoomGroup::hasRoom (checks if a room exists)
function RTBCC_RoomGroup::hasRoom(%this,%name)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      if(%this.getObject(%i).name $= %name)
         return true;
   }
   return false;
}

//- RTBCC_RoomGroup::getRoomByName (returns a room by name)
function RTBCC_RoomGroup::getRoomByName(%this,%name)
{
   if(!%this.hasRoom(%name))
      return false;
      
   for(%i=0;%i<%this.getCount();%i++)
   {
      %room = %this.getObject(%i);
      if(%room.name $= %name)
         return %room;
   }
   return false;
}

//- RTBCC_RoomGroup::removeRoomByName (removes a room by name)
function RTBCC_RoomGroup::removeRoomByName(%this,%name)
{
   // to be implemented
}

//*********************************************************
//* Room Rendering & Manipulation
//*********************************************************
//- RTBCC_Chat_Swatch::clear (resizes the swatch to the correct extent)
function RTBCC_Chat_Swatch::clear(%this)
{
   Parent::clear(%this);
   %this.resize(1,1,194,getWord(RTBCC_Chat_Scroll.extent,1)-2);
}

//- RTBCC_Chat_Swatch::reshape (shapes the size of the chat swatch to be high enough)
function RTBCC_Chat_Swatch::reshape(%this)
{
   if(%this.getLowestPoint() < (getWord(RTBCC_Chat_Scroll.extent,1)-2))
      %this.resize(1,getWord(%this.position,1),194,getWord(RTBCC_Chat_Scroll.extent,1)-2);
   else
      %this.resize(1,getWord(%this.position,1),194,%this.getLowestPoint());
}

//- RTBCC_RoomManager::render (renders the room panel)
function RTBCC_RoomManager::render(%this)
{
   RTBCC_Chat_Swatch.clear();
   
   for(%i=0;%i<%this.getCount();%i++)
   {
      %group = %this.getObject(%i);
      if(%group.getCount() <= 0)
         continue;
         
      %group.render();
   }
}

//- RTBCC_Room::join (tries to join a room)
function RTBCC_Room::join(%this)
{
   RTB_ConnectClient.messageBox(%this.name, "Attempting to join room ...");
   RTBCC_Socket.joinRoom(%this.name);
}

//- RTBCC_Room::leave (tries to leave a room)
function RTBCC_Room::leave(%this,%direct)
{
   if(!%direct)
   {
      %room = RTBCC_RoomSessionManager.getRoomByName(%this.name);
      if(%room.window.modalSwatch.isVisible())
      {
         %room.closeModalWindow();
         return;
      }
   }
   
   RTBCC_RoomSessionManager.getRoomByName(%this.name).destroy();
   RTBCC_Socket.leaveRoom(%this.name);
   
   if(RTB_ConnectClient.isOpen() && RTB_ConnectClient.currPane $= "RTBCC_Window_Chat")
      RTBCC_RoomManager.refresh();
}

//- RTBCC_Room::isRendered (checks to see if the room is rendered already)
function RTBCC_Room::isRendered(%this)
{
   if(!isObject(%this.gui_container))
      return false;
      
   if(%this.gui_container.getGroup().getID() !$= %this.getGroup().container.getID())
      return false;
      
   return true;
}

//- RTBCC_Room::render (attempts to render the room)
function RTBCC_Room::render(%this)
{
   if(%this.isRendered())
   {
      %ml = %this.gui_container.userText;
      if(%this.users $= 1)
         %ml.setText(%ml.prepend @ %this.users @ " User");
      else
         %ml.setText(%ml.prepend @ %this.users @ " Users");
   }
   else
   {
      if(!%this.getGroup().isRendered())
         %this.getGroup().render();
      else
      {
         %position = 32;
         //%this.getGroup().sort();
         for(%i=0;%i<%this.getGroup().getCount();%i++)
         {
            %room = %this.getGroup().getObject(%i);
            if(!%room.isRendered() && %room !$= %this)
               continue;
               
            if(%this $= %room)
            {
               %this.getGroup().container.conditionalShiftY(%position,40);
               %this.renderInPlace("16" SPC %position);
               %this.getGroup().container.extent = vectorAdd(%this.getGroup().container.extent,"0 40");
               RTBCC_Chat_Swatch.conditionalShiftY(getWord(%this.getGroup().container.position,1) + 1,40);
            }
            else
               %position += 40;
         }
         RTBCC_Chat_Swatch.reshape();
      }
   }
}

//- RTBCC_Room::renderInPlace (renders a room taking a position argument)
function RTBCC_Room::renderInPlace(%this,%position)
{
   if(%this.isRendered())
      return;
      
   %swatch = %this.getGroup().container;
   
   if(%this.type == 1)
   {
      %container = new GuiBitmapCtrl()
      {
         position = %position;
         extent = "164 36";
         bitmap = $RTB::Path @ "images/ui/roomBody";
         
         new GuiBitmapCtrl()
         {
            position = "5 5";
            extent = "16 16";
            bitmap = $RTB::Path @ "images/icons/" @ %this.icon;
         };
         
         new GuiMLTextCtrl()
         {
            position = "27 7";
            extent = "150 12";
            text = "<color:FFFFFF><font:Verdana Bold:12>" @ %this.name;
         };
         
         new GuiMLTextCtrl()
         {
            position = "29 20";
            extent = "150 12";
            prepend = "<color:DDDDDD><font:Arial:12>";
         };
         
         new GuiSwatchCtrl()
         {
            position = "0 0";
            extent = "164 36";
            color = "255 255 255 50";
            visible = false;
         };
         
         new GuiMouseEventCtrl()
         {
            position = "0 0";
            extent = "164 36";
            
            room = %this;
            
            eventType = "roomSelect";
            eventCallbacks = "1111000";
         };
      };
      %swatch.add(%container);
      
      %container.userText = %container.getObject(2);
   }
   else if(%this.type == 2)
   {
      %container = new GuiSwatchCtrl()
      {
         position = "2" SPC getWord(%position,1);
         extent = "180 36";
         color = "0 0 0 0";
         
         new GuiBitmapCtrl()
         {
            position = "0 0";
            extent = "180 31";
            bitmap = $RTB::Path @ "images/ui/roomBodyBack";
            visible = false;
         };
         
         new GuiBitmapCtrl()
         {
            position = "5 6";
            extent = "16 16";
            bitmap = $RTB::Path @ "images/icons/lock";
            visible = true;
         };
         
         new GuiMLTextCtrl()
         {
            position = "27 3";
            extent = "150 12";
            text = "<color:666666><font:Verdana Bold:12>" @ %this.name;
         };
         
         new GuiMLTextCtrl()
         {
            position = "29 15";
            extent = "150 12";
            prepend = "<color:888888><font:Arial:12>";
         };
         
         new GuiSwatchCtrl()
         {
            position = "26 35";
            extent = "148 1";
            minExtent = "1 1";
            color = "200 200 200 255";
         };
         
         new GuiBitmapCtrl()
         {
            position = "161 1";
            extent = "16 16";
            bitmap = $RTB::Path @ "images/icons/bullet_go";
         };
         
         new GuiSwatchCtrl()
         {
            position = "0 0";
            extent = "180 36";
            color = "255 255 255 100";
            visible = false;
         };
         
         new GuiMouseEventCtrl()
         {
            position = "0 0";
            extent = "180 36";
            
            room = %this;
            
            eventType = "roomSelectOther";
            eventCallbacks = "1111000";
         };
      };
      %swatch.add(%container);
      
      %container.userText = %container.getObject(3);
   }
   %this.gui_container = %container;
   
   %this.render();
}

//- Event_roomSelect::onMouseEnter (onMouseEnter callback)
function Event_roomSelect::onMouseEnter(%this)
{
   %this.getGroup().getObject(3).color = "255 255 255 50";
   %this.getGroup().getObject(3).setVisible(true);
}

//- Event_roomSelect::onMouseLeave (onMouseLeave callback)
function Event_roomSelect::onMouseLeave(%this)
{
   %this.getGroup().getObject(3).setVisible(false);
}

//- Event_roomSelect::onMouseDown (onMouseDown callback)
function Event_roomSelect::onMouseDown(%this)
{
   %this.getGroup().getObject(3).color = "255 255 255 100";
}

//- Event_roomSelect::onMouseUp (onMouseUp callback)
function Event_roomSelect::onMouseUp(%this)
{
   %this.getGroup().getObject(3).color = "255 255 255 50";
   
   if(RTBCC_RoomSessionManager.hasRoom(%this.room.name))
      RTBCC_RoomSessionManager.getRoomByName(%this.room.name).focus();
   else
      %this.room.join();
}

//- Event_roomSelectOther::onMouseEnter (onMouseEnter callback)
function Event_roomSelectOther::onMouseEnter(%this)
{
   %this.getGroup().getObject(0).setVisible(true);
   %this.getGroup().getObject(6).setVisible(false);
}

//- Event_roomSelectOther::onMouseLeave (onMouseLeave callback)
function Event_roomSelectOther::onMouseLeave(%this)
{
   %this.getGroup().getObject(0).setVisible(false);
   %this.getGroup().getObject(6).setVisible(false);
}

//- Event_roomSelectOther::onMouseDown (onMouseDown callback)
function Event_roomSelectOther::onMouseDown(%this)
{
   %this.getGroup().getObject(6).setVisible(true);
}

//- Event_roomSelectOther::onMouseUp (onMouseUp callback)
function Event_roomSelectOther::onMouseUp(%this)
{
   %this.getGroup().getObject(6).setVisible(false);
}

//- RTBCC_Room::rerender (rerenders the room if it's already rendered)
function RTBCC_Room::rerender(%this)
{
   if(%this.isRendered())
      %this.unrender();
   %this.render();
}

//- RTBCC_Room::unrender (unrenders the room and parent group if empty)
function RTBCC_Room::unrender(%this)
{
   if(!%this.isRendered())
      return;
      
   %position = getWord(%this.gui_container.position,1);
   
   %this.gui_container.delete();

   %this.getGroup().container.conditionalShiftY(%position,-40);
   %this.getGroup().container.extent = vectorSub(%this.getGroup().container.extent,"0 40");
   RTBCC_Chat_Swatch.conditionalShiftY(getWord(%this.getGroup().container.position,1)+1,-40);
   
   RTBCC_Chat_Swatch.reshape();
}

//- RTBCC_RoomGroup::isRendered (checks to see if the room group is rendered already)
function RTBCC_RoomGroup::isRendered(%this)
{
   if(!isObject(%this.container))
      return false;
      
   if(!isObject(%this.container.getGroup()) || %this.container.getGroup().getID() !$= RTBCC_Chat_Swatch.getID())
      return false;
      
   return true;
}

//- RTBCC_RoomGroup::render (attempts to render the rogoom group and rooms)
function RTBCC_RoomGroup::render(%this)
{
   if(%this.isRendered())
      return;
      
   if(%this.getCount() <= 0)
      return;
      
   %position = 0;
   for(%i=0;%i<RTBCC_RoomManager.getCount();%i++)
   {
      %group = RTBCC_RoomManager.getObject(%i);
      if(!%group.isRendered() && %this !$= %group)
         continue;
         
      if(%this $= %group)
      {
         %extent = ((%group.getCount() + 1) * 40) + 4;
         RTBCC_Chat_Swatch.conditionalShiftY(%position,%extent);
         
         %group.renderInPlace("0" SPC %position);
         
         for(%j=0;%j<%group.getCount();%j++)
         {
            %position = "18" SPC (%j * 40) + 32;
            %group.getObject(%j).renderInPlace(%position);
         }
      }
      else
         %position += getWord(%group.container.extent,1);
   }
   RTBCC_Chat_Swatch.reshape();
}

//- RTBCC_RoomGroup::renderInPlace (renders a room group taking a position argument)
function RTBCC_RoomGroup::renderInPlace(%this,%position)
{
   %extent = (%this.getCount() * 40) + 30;
   %this.container = new GuiSwatchCtrl()
   {
      position = %position;
      extent = "194" SPC %extent;
      color = "0 0 0 0";
      
      new GuiBitmapCtrl()
      {
         position = "2 2";
         extent = "180 26";
         bitmap = $RTB::Path @ "images/ui/roomGroupHeader";
         
         new GuiBitmapCtrl()
         {
            position = "5 5";
            extent = "16 16";
            bitmap = $RTB::Path @ "images/icons/" @ %this.icon;
         };
      
         new GuiMLTextCtrl()
         {
            position = "27 7";
            extent = "150 12";
            text = "<color:EEEEEE><font:Verdana Bold:12>" @ %this.name;
            
            selectable = false;
         };
      };
   };
   RTBCC_Chat_Swatch.add(%this.container);
}

//- RTBCC_RoomGroup::rerender (rerenders the room group if it's already rendered)
function RTBCC_RoomGroup::rerender(%this)
{
   if(%this.isRendered())
      %this.unrender();
   %this.render();
}

//- RTBCC_RoomGroup::unrender (unrenders a room group and its user items)
function RTBCC_RoomGroup::unrender(%this)
{
   if(!%this.isRendered())
      return;

   %position = getWord(%this.container.position,1);
   %extent = getWord(%this.container.extent,1);
   
   %this.container.delete();
   RTBCC_Chat_Swatch.conditionalShiftY(%position,"-"@%extent);
   RTBCC_Chat_Swatch.reshape();
}

//*********************************************************
//* Room Session Manager Implementation
//*********************************************************
//- RTBCC_createRoomSessionManager (creates a room session manager)
function RTBCC_createRoomSessionManager()
{
   if(isObject(RTBCC_RoomSessionManager))
   {
      RTBCC_RoomSessionManager.destroy();
      RTBCC_RoomSessionManager.delete();
   }
   %manager = new ScriptGroup(RTBCC_RoomSessionManager);
   RTBGroup.add(%manager);
   
   return %manager;
}

//- RTBCC_RoomSessionManager::createSession (creates a session and manifest)
function RTBCC_RoomSessionManager::createSession(%this,%name)
{
   if(%this.hasRoom(%name))
      return %this.getRoomByName(%name);
      
   %session = new ScriptGroup()
   {
      class = "RTBCC_RoomSession";
      
      name = %name;
      room = RTBCC_RoomManager.getRoomByName(%name);
   };
   %this.add(%session);
   
   %manifest = new ScriptGroup()
   {
      class = "RTBCC_RoomSessionManifest";
      
      session = %session;
   };
   %session.add(%manifest);
   %session.manifest = %manifest;
   
   %session.render();
   
   return %session;
}

//- RTBCC_RoomSessionManager::resetCursor (resets cursor to active room or best alternative)
function RTBCC_RoomSessionManager::resetCursor(%this)
{
   if(isObject(%this.lastFocus) && %this.isMember(%this.lastFocus))
      %this.lastFocus.focus();
   else if(%this.getCount() > 0)
   {
      for(%i=%this.getCount()-1;%i>=0;%i--)
      {
         %session = %this.getObject(%i);
         if(%session.isRendered())
         {
            %session.focus();
            break;
         }
      }
   }
}

//- RTBCC_RoomSessionManager::hasRoom (checks for a room session by name)
function RTBCC_RoomSessionManager::hasRoom(%this,%name)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      if(%this.getObject(%i).name $= %name)
         return true;
   }
   return false;
}

//- RTBCC_RoomSessionManager::getRoomByName (returns a room session by name)
function RTBCC_RoomSessionManager::getRoomByName(%this,%name)
{
   if(!%this.hasRoom(%name))
      return false;
      
   for(%i=0;%i<%this.getCount();%i++)
   {
      %object = %this.getObject(%i);
      if(%object.name $= %name)
         return %object;
   }
   return false;
}

//- RTBCC_RoomSessionManager::reclaim (re-focuses room pointer incase things got frisky)
function RTBCC_RoomSessionManager::reclaim(%this)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %session = %this.getObject(%i);
      %session.room = RTBCC_RoomManager.getRoomByName(%session.name);
   }
}

//- RTBCC_RoomSessionManager::destroy (destroys all room sessions)
function RTBCC_RoomSessionManager::destroy(%this)
{
   while(%this.getCount() >= 1)
   {
      %this.getObject(0).destroy();
   }
}

//*********************************************************
//* Room Session Implementation
//*********************************************************
//- RTBCC_RoomSession::onNotice (handles room session notice)
function RTBCC_RoomSession::onNotice(%this,%parser,%packet)
{
   if(%packet.attrib["type"] $= "users")
   {
      for(%i=0;%i<%packet.children;%i++)
      {
         %user = %packet.child[%i];
         %this.manifest.addUser(%user.attrib["id"],%user.attrib["name"],%user.attrib["rank"],%user.attrib["icon"],%user.attrib["blocked"]);
      }
      
      if(%packet.attrib["end"] $= "1")
      {
         %this.manifest.loading = false;
         %this.manifest.render();
      }
      
      %user = %this.manifest.getByID(RTB_ConnectClient.client_id);
      if(%user && %user.rank >= 2)
         %this.setAdminDisplay(1);
      else
         %this.setAdminDisplay(0);
   }
   else if(%packet.attrib["type"] $= "join")
   {
      %user = %packet.child[0];
      %user = %this.manifest.addUser(%user.attrib["id"],%user.attrib["name"],%user.attrib["rank"],%user.attrib["icon"],%user.attrib["blocked"]);
      if(!%this.manifest.loading)
         %user.render();
      
      if(%this.options.join_message)
         %this.writeNotice(%user.name @ " has joined the room.");
      if(%this.options.join_popup)
         RTBCC_NotificationManager.push(%this.room.name,%user.name @ " joined.","comment_add","joinleave_"@%user.name);
   }
   else if(%packet.attrib["type"] $= "kick")
   {
      %id = %packet.child[0].attrib["id"];
      %user = %this.manifest.getByID(%id);
      %kicker = %this.manifest.getByID(%packet.attrib["user"]);

      if(%packet.attrib["reason"] !$= "")
         %this.writeColor(%kicker.name @ " has kicked " @ %user.name @" from the room. ("@%packet.attrib["reason"]@")", "FF0000");
      else
         %this.writeColor(%kicker.name @ " has kicked " @ %user.name @" from the room.", "FF0000");
         
      if(%user.id $= RTB_ConnectClient.client_id)
      {
         RTBCC_NotificationManager.push(%this.room.name,"You have been kicked.","delete");
         if(%packet.attrib["reason"] !$= "")
            %this.messageBoxError("You were kicked!","You were kicked from the room by "@%kicker.name@".<br><br>"@%packet.attrib["reason"],%this@".destroy();");
         else
            %this.messageBoxError("You were kicked!","You were kicked from the room by "@%kicker.name@".",%this@".destroy();");
      }
   }
   else if(%packet.attrib["type"] $= "rank")
   {
      %id = %packet.child[0].attrib["id"];
      %user = %this.manifest.getByID(%id);
      
      %oldRank = %user.rank;
      %user.rank = %packet.child[0].attrib["rank"];
      %user.icon = %packet.child[0].attrib["icon"];
      if(!%this.manifest.loading)
      {
         %user.getGroup().sort();
         %user.rerender();
      }
      
      %changer = %this.manifest.getByID(%packet.attrib["user"]);
      
      if(%user.id $= RTB_ConnectClient.client_id)
         if(%user.rank >= 2)
            %this.setAdminDisplay(1);
         else
            %this.setAdminDisplay(0);
      
      %rank[0] = "a normal user";
      %rank[1] = "a moderator";
      %rank[2] = "an administrator";
      if(%oldRank !$= %user.rank)
         %this.writeColor(%changer.name @ " has made " @ %user.name SPC %rank[%user.rank] @ ".","0099FF");
      
      if(%id $= RTB_ConnectClient.client_id)
         if(isObject(%this.gui_userMenu))
            %this.gui_userMenu.user.closeMenu();
   }
   else if(%packet.attrib["type"] $= "leave")
   {
      %id = %packet.child[0].attrib["id"];
      %user = %this.manifest.getByID(%id);

      if(%this.options.leave_message)
         %this.writeNotice(%user.name @ " has left the room.");
      if(%this.options.leave_popup)
         RTBCC_NotificationManager.push(%this.room.name,%user.name @ " left.","comment_delete","joinleave_"@%user.name);
      
      %this.manifest.removeByID(%id);
   }
   else if(%packet.attrib["type"] $= "motd")
   {
      %this.writeNotice(%packet.cData);
   }
   else if(%packet.attrib["type"] $= "motd_change")
   {
      %user = %this.manifest.getByID(%packet.attrib["user"]);
      %this.writeNotice(%user.name @ " changed the welcome message to \""@ %packet.cData @"\"");
   }
}

//- RTBCC_RoomSession::send (sends whatever is in the text box)
function RTBCC_RoomSession::send(%this)
{
   %text = %this.window.input.getValue();
   if(%text $= "")
      return;
   
   RTBCC_RoomSessionManager.lastFocus = %this;
   RTBCC_RoomSessionManager.lastFocusTime = getSimTime();
   
   %this.window.input.setValue("");
   
   if(getSubStr(%text,0,1) $= "/")
   {
      if(firstWord(%text) $= "/me" || firstWord(%text) $= "/action")
      {
         %this.writeAction(RTB_ConnectClient.client_name,parseLinks(stripMLControlChars(restWords(%text))));
         RTBCC_Socket.sendMessage(%this.name,restWords(%text),true);
      }
   }
   else
   {
      %this.writeMessage("<color:FF6600>"@RTB_ConnectClient.client_name,parseLinks(stripMLControlChars(%text)));
      RTBCC_Socket.sendMessage(%this.name,%text);
   }
   
   %this.focus();
   
   %this.window.scroll.scrollToBottom();
}

//- RTBCC_RoomSession::receive (handles a message packet)
function RTBCC_RoomSession::receive(%this,%message)
{
   if(!%this.isRendered())
      %this.render();
   
   %from = %message.attrib["from"];
   if(!%this.manifest.hasUser(%from))
      return;
      
   %user = %this.manifest.getByID(%from);
   %name = %user.name;
   if(RTBCC_Roster.hasID(%user.id) && RTBCC_Roster.getByID(%user.id).state !$= "pending_to")
      %name = "<color:228822>"@%name;
      
   if(%message.attrib["type"] $= "action")
      %this.writeAction(%user.name,%message.find("body").cData);
   else
      %this.writeMessage(%name,%message.find("body").cData);
}

//- RTBCC_Session::writeMessage (adds a user-sent message to the bottom of the ml text)
function RTBCC_RoomSession::writeMessage(%this,%sender,%message)
{
   if(RTBCO_getPref("CC::ChatLogging"))
      %this.log(%sender@": "@%message);
      
   %message = "<font:Verdana Bold:12>"@%sender@"<font:Verdana:12><color:444444>: "@%message;
   
   if(RTBCO_getPref("CC::ShowTimestamps"))
      %message = "<font:Verdana Bold:12>["@getSubStr(getWord(getDateTime(),1),0,8)@"] " @ %message;
      
   %this.write(%message);
}

//- RTBCC_RoomSession::writeAction (adds an action message to the bottom of the ml text)
function RTBCC_RoomSession::writeAction(%this,%sender,%message)
{
   if(RTBCO_getPref("CC::ChatLogging"))
      %this.log("* "@%sender@" "@%message);
      
   %message = "<font:Verdana Bold:12><color:CC00CC>* "@%sender@" <font:Verdana:12>"@%message@"<color:444444>";
   
   if(RTBCO_getPref("CC::ShowTimestamps"))
      %message = "<font:Verdana Bold:12>["@getSubStr(getWord(getDateTime(),1),0,8)@"] " @ %message;
   
   %this.write(%message);
}

//- RTBCC_RoomSession::writeInfo (adds an information message to the bottom of the ml text)
function RTBCC_RoomSession::writeInfo(%this,%message)
{
   if(RTBCO_getPref("CC::ChatLogging"))
      %this.log("* "@%message);
      
   %message = "<font:Verdana:12><color:00AA00>* "@%message@"<color:444444>";
   
   if(RTBCO_getPref("CC::ShowTimestamps"))
      %message = "<font:Verdana Bold:12>["@getSubStr(getWord(getDateTime(),1),0,8)@"] " @ %message;
   
   %this.write(%message);
}

//- RTBCC_RoomSession::writeColor (adds a colored message to the bottom of the ml text)
function RTBCC_RoomSession::writeColor(%this,%message,%color)
{
   if(RTBCO_getPref("CC::ChatLogging"))
      %this.log("* "@%message);
      
   %message = "<font:Verdana:12><color:"@%color@">* "@%message@"<color:444444>";
   
   if(RTBCO_getPref("CC::ShowTimestamps"))
      %message = "<font:Verdana Bold:12>["@getSubStr(getWord(getDateTime(),1),0,8)@"] " @ %message;
   
   %this.write(%message);
}

//- RTBCC_RoomSession::writeNotice (adds a notice message to the bottom of the ml text)
function RTBCC_RoomSession::writeNotice(%this,%message)
{
   if(RTBCO_getPref("CC::ChatLogging"))
      %this.log("* "@%message);
      
   %message = "<font:Verdana:12><color:666666>* "@%message@"<color:444444>";
   
   if(RTBCO_getPref("CC::ShowTimestamps"))
      %message = "<font:Verdana Bold:12>["@getSubStr(getWord(getDateTime(),1),0,8)@"] " @ %message;
   
   %this.write(%message);
}

//- RTBCC_RoomSession::writeError (adds an error message to the bottom of the ml text)
function RTBCC_RoomSession::writeError(%this,%message)
{
   %message = "<color:FF0000>* "@%message@"<color:444444>";
   
   %this.write(%message);
}

//- RTBCC_RoomSession::write (adds a line to the bottom of the ml text)
function RTBCC_RoomSession::write(%this,%line)
{
   if(!%this.isRendered())
      return;
      
   %scroll = %this.window.scroll;
   %display = %this.window.display;
   
   %position = getWord(%display.position,1);
   if((getWord(%display.extent,1)+getWord(%display.position,1)) <= (getWord(%scroll.extent,1)-1))
      %atBottom = true;
   
   if(%display.getValue() $= "")
      %display.setValue(%line);
   else
      %display.setValue(%display.getValue()@"\n"@%line);
      
   if(RTB_Overlay.isAwake())
      %display.forceReflow();
      
   %display.setCursorPosition(strLen(%display.getValue()));
   
   if(%atBottom)
      %scroll.scrollToBottom();
   else
      %display.resize(getWord(%display.position,0),%position,getWord(%display.extent,0),getWord(%display.extent,1));
}

//- RTBCC_RoomSession::log (logs a message into the chat logging)
function RTBCC_RoomSession::log(%this,%message)
{
   if(!isObject(%this.logger))
   {
      %this.logger = new FileObject();
      %this.logger.openForAppend("config/client/rtb/logs/rooms/"@getSafeVariableName(%this.name)@".txt");
      %this.logger.writeLine("");
      %this.logger.writeLine(getDateTime());
      %this.logger.writeLine("--");
      %this.logger.close();
   }
   %this.logger.openForAppend("config/client/rtb/logs/rooms/"@getSafeVariableName(%this.name)@".txt");
   %this.logger.writeLine("["@lastWord(getDateTime())@"] "@stripMLControlChars(%message));
   %this.logger.close();
   
   RTBGroup.add(%this.logger);
}

//- RTBCC_RoomSession::setAdminDisplay (enables/disables admin button)
function RTBCC_RoomSession::setAdminDisplay(%this,%status)
{
   if(%status $= 1)
   {
      if(%this.adminDisplay)
         return;
      
      %this.adminDisplay = 1;
      
      %ctrl = %this.window.inputContainer;
      %ctrl.resize(getWord(%ctrl.position,0),getWord(%ctrl.position,1),getWord(%ctrl.extent,0)-25,getWord(%ctrl.extent,1));
   }
   else
   {
      if(!%this.adminDisplay)
         return;
      
      %this.adminDisplay = 0;
      %ctrl = %this.window.inputContainer;
      %ctrl.resize(getWord(%ctrl.position,0),getWord(%ctrl.position,1),getWord(%ctrl.extent,0)+25,getWord(%ctrl.extent,1));
   }
}

//- RTBCC_RoomSession::focus (brings the window into user focus)
function RTBCC_RoomSession::focus(%this)
{
   if(%this.isRendered())
   {
      RTB_Overlay.pushToBack(%this.window);
      %this.window.input.makeFirstResponder(1);
   }
}

//- RTBCC_RoomSession::render (renders a room window for the session)
function RTBCC_RoomSession::render(%this)
{
   if(%this.isRendered())
   {
      %this.focus();
      return;
   }
      
   %window = new GuiWindowCtrl()
   {
      profile = GuiWindowProfile;
      position = "0 0";
      extent = "500 342";
      minExtent = "450 250";
      text = %this.room.getGroup().name @ " - " @ %this.name;
      resizeWidth = true;
      resizeHeight = true;
      canMove = true;
      canClose = true;
      canMinimize = false;
      canMaximize = false;
      title = %this.room.getGroup().name @ " - " @ %this.name;
      
      new GuiBitmapBorderCtrl()
      {
         profile = RTB_ContentBorderProfile;
         horizSizing = "width";
         vertSizing = "height";
         position = "7 30";
         extent = "334 280";
         
         new GuiSwatchCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "width";
            vertSizing = "height";
            position = "3 3";
            extent = "328 274";
            color = "255 255 255 255";

            new GuiScrollCtrl()
            {            
               profile = RTB_ScrollProfile;
               horizSizing = "width";
               vertSizing = "height";
               position = "1 1";
               extent = "326 271";
               hScrollBar = "alwaysOff";
               
               dSet = "window";
               dName = "scroll";
                  
               new GuiMLTextCtrl()
               {
                  profile = RTB_MLEditProfile;
                  horizSizing = "width";
                  vertSizing = "height";
                  position = "1 1";
                  extent = "312 12";
                  
                  dSet = "window";
                  dName = "display";
               };
            };
         };
      };
      
      new GuiBitmapBorderCtrl()
      {
         profile = RTB_ContentBorderProfile;
         horizSizing = "left";
         vertSizing = "height";
         position = "344 30";
         extent = "150 280";
         
         new GuiSwatchCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "width";
            vertSizing = "height";
            position = "3 3";
            extent = "144 274";
            color = "255 255 255 255";
            
            dSet = "window";
            dName = "userPane";

            new GuiScrollCtrl()
            {            
               profile = RTB_ScrollProfile;
               horizSizing = "width";
               vertSizing = "height";
               position = "1 1";
               extent = "142 272";
               hScrollBar = "alwaysOff";
               
               dSet = "window";
               dName = "userScroll";
               
               new GuiSwatchCtrl()
               {
                  profile = GuiDefaultProfile;
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "1 1";
                  extent = "140 0";
                  minExtent = "140 0";
                  color = "255 255 255 255";
                  
                  dSet = "window";
                  dName = "userSwatch";
               };
            };
         };
         
         new GuiSwatchCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "width";
            vertSizing = "height";
            position = "3 3";
            extent = "144 274";
            color = "255 255 255 255";
            visible = false;
            
            dSet = "window";
            dName = "optsPane";
            
            new GuiScrollCtrl()
            {            
               profile = RTB_ScrollProfile;
               horizSizing = "width";
               vertSizing = "height";
               position = "1 1";
               extent = "142 272";
               hScrollBar = "alwaysOff";
               
               dSet = "window";
               dName = "optsScroll";
               
               new GuiSwatchCtrl()
               {
                  profile = GuiDefaultProfile;
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "1 1";
                  extent = "140 258";
                  minExtent = "140 0";
                  color = "255 255 255 255";
                  
                  dSet = "window";
                  dName = "optsSwatch";
            
                  new GuiBitmapCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "6 2";
                     extent = "16 16";
                     bitmap = $RTB::Path@"images/icons/wrench";
                  };
                  
                  new GuiMLTextCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "26 4";
                     extent = "150 12";
                     text = "<font:Verdana Bold:12><color:444444>General";
                     selectable = false;
                  };
                  
                  new GuiBitmapCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "4 21";
                     extent = "121 2";
                     bitmap = $RTB::Path@"images/ui/dottedLine";
                     wrap = true;
                  };
                  
                  new GuiMLTextCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "7 28";
                     extent = "100 12";
                     text = "<font:Verdana:12><color:888888>Auto-join this room";
                     selectable = false;
                  };
                  
                  new GuiCheckboxCtrl()
                  {
                     profile = RTB_CheckboxProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "109 27";
                     extent = "16 16";
                     text = " ";
                     groupNum = "-1";
                     buttonType = "ToggleButton";
                     command = "RTBCC_RoomOptionsManager.storeRoomSettings("@%this@");";
                     
                     var = "autojoin";
                     def = 0;
                  };
                  
                  new GuiBitmapCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "6 54";
                     extent = "16 16";
                     bitmap = $RTB::Path@"images/icons/note";
                  };
                  
                  new GuiMLTextCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "26 56";
                     extent = "150 12";
                     text = "<font:Verdana Bold:12><color:444444>Notifications";
                     selectable = false;
                  };
                  
                  new GuiBitmapCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "4 73";
                     extent = "121 2";
                     bitmap = $RTB::Path@"images/ui/dottedLine";
                     wrap = true;
                  };
                  
                  new GuiMLTextCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "7 78";
                     extent = "126 12";
                     text = "<font:Verdana:12><color:888888>When a user joins ...";
                     selectable = false;
                  };
                  
                  new GuiMLTextCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "8 92";
                     extent = "94 12";
                     text = "<font:Verdana:12><color:AAAAAA><just:right>Sound";
                     selectable = false;
                  };
                  
                  new GuiCheckboxCtrl()
                  {
                     profile = RTB_CheckboxProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "109 91";
                     extent = "16 16";
                     text = " ";
                     groupNum = "-1";
                     buttonType = "ToggleButton";
                     command = "RTBCC_RoomOptionsManager.storeRoomSettings("@%this@");";
                     
                     var = "join_sound";
                     def = 0;
                  };
                  
                  new GuiMLTextCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "8 108";
                     extent = "94 12";
                     text = "<font:Verdana:12><color:AAAAAA><just:right>Popup";
                     selectable = false;
                  };
                  
                  new GuiCheckboxCtrl()
                  {
                     profile = RTB_CheckboxProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "109 107";
                     extent = "16 16";
                     text = " ";
                     groupNum = "-1";
                     buttonType = "ToggleButton";
                     command = "RTBCC_RoomOptionsManager.storeRoomSettings("@%this@");";
                     
                     var = "join_popup";
                     def = 0;
                  };
                  
                  new GuiMLTextCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "8 123";
                     extent = "94 12";
                     text = "<font:Verdana:12><color:AAAAAA><just:right>Chat Message";
                     selectable = false;
                  };
                  
                  new GuiCheckboxCtrl()
                  {
                     profile = RTB_CheckboxProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "109 122";
                     extent = "16 16";
                     text = " ";
                     groupNum = "-1";
                     buttonType = "ToggleButton";
                     command = "RTBCC_RoomOptionsManager.storeRoomSettings("@%this@");";
                     
                     var = "join_message";
                     def = 1;
                  };
                  
                  new GuiMLTextCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "7 140";
                     extent = "126 12";
                     text = "<font:Verdana:12><color:888888>When a user leaves ...";
                     selectable = false;
                  };
                  
                  new GuiMLTextCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "8 154";
                     extent = "94 12";
                     text = "<font:Verdana:12><color:AAAAAA><just:right>Sound";
                     selectable = false;
                  };
                  
                  new GuiCheckboxCtrl()
                  {
                     profile = RTB_CheckboxProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "109 153";
                     extent = "16 16";
                     text = " ";
                     groupNum = "-1";
                     buttonType = "ToggleButton";
                     command = "RTBCC_RoomOptionsManager.storeRoomSettings("@%this@");";
                     
                     var = "leave_sound";
                     def = 0;
                  };
                  
                  new GuiMLTextCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "8 170";
                     extent = "94 12";
                     text = "<font:Verdana:12><color:AAAAAA><just:right>Popup";
                     selectable = false;
                  };
                  
                  new GuiCheckboxCtrl()
                  {
                     profile = RTB_CheckboxProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "109 169";
                     extent = "16 16";
                     text = " ";
                     groupNum = "-1";
                     buttonType = "ToggleButton";
                     command = "RTBCC_RoomOptionsManager.storeRoomSettings("@%this@");";
                     
                     var = "leave_popup";
                     def = 0;
                  };
                  
                  new GuiMLTextCtrl()
                  {
                     profile = GuiDefaultProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "8 185";
                     extent = "94 12";
                     text = "<font:Verdana:12><color:AAAAAA><just:right>Chat Message";
                     selectable = false;
                  };
                  
                  new GuiCheckboxCtrl()
                  {
                     profile = RTB_CheckboxProfile;
                     horizSizing = "right";
                     vertSizing = "bottom";
                     position = "109 184";
                     extent = "16 16";
                     text = " ";
                     groupNum = "-1";
                     buttonType = "ToggleButton";
                     command = "RTBCC_RoomOptionsManager.storeRoomSettings("@%this@");";
                     
                     var = "leave_message";
                     def = 1;
                  };
               };
            };
         };
      };
      
      new GuiBitmapButtonCtrl() {
         profile = "GuiDefaultProfile";
         horizSizing = "left";
         vertSizing = "top";
         position = "466 308";
         extent = "24 27";
         command = "";
         text = " ";
         groupNum = "2";
         buttonType = "RadioButton";
         bitmap = $RTB::Path@"images/ui/buttons/connectClient/tabUserList";
         mKeepCached = "1";
      };
      
      new GuiBitmapButtonCtrl() {
         profile = "GuiDefaultProfile";
         horizSizing = "left";
         vertSizing = "top";
         position = "439 308";
         extent = "24 27";
         command = "";
         text = " ";
         groupNum = "2";
         buttonType = "RadioButton";
         bitmap = $RTB::Path@"images/ui/buttons/connectClient/tabSettings";
         mKeepCached = "1";
      };
      
      new GuiBitmapBorderCtrl()
      {
         profile = RTB_ContentBorderProfile;
         horizSizing = "left";
         vertSizing = "top";
         position = "414 313";
         extent = "22 22";
         
         new GuiSwatchCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "width";
            vertSizing = "height";
            position = "3 3";
            extent = "16 16";
            color = "255 255 255 255";
         };
         
         new GuiBitmapCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "left";
            vertSizing = "bottom";
            position = "3 3";
            extent = "16 16";
            bitmap = $RTB::Path@"images/icons/shield_gold";
         };
         
         new GuiBitmapButtonCtrl() {
            profile = "GuiDefaultProfile";
            horizSizing = "left";
            vertSizing = "bottom";
            position = "2 2";
            extent = "18 18";
            command = %this@".openAdminPanel();";
            text = " ";
         };
      };
      
      new GuiBitmapBorderCtrl()
      {
         profile = RTB_ContentBorderProfile;
         horizSizing = "width";
         vertSizing = "top";
         position = "7 313";
         extent = "429 22";
         
         dSet = "window";
         dName = "inputContainer";
         
         new GuiSwatchCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "width";
            vertSizing = "height";
            position = "3 3";
            extent = "423 16";
            color = "255 255 255 255";
         };
         
         new GuiBitmapCtrl()
         {
            profile = GuiDefaultProfile;
            horizSizing = "left";
            vertSizing = "bottom";
            position = "410 2";
            extent = "16 16";
            bitmap = $RTB::Path@"images/icons/bullet_go";
         };
         
         new GuiBitmapButtonCtrl() {
            profile = "GuiDefaultProfile";
            horizSizing = "left";
            vertSizing = "bottom";
            position = "410 2";
            extent = "16 16";
            command = %this@".send();";
            text = " ";
         };
      };
      
      new GuiSwatchCtrl() {
         profile = GuiDefaultProfile;
         horizSizing = "width";
         vertSizing = "height";
         position = "6 27";
         extent = "490 310";
         color = "200 200 200 150";
         visible = "0";
         
         dSet = "window";
         dName = "modalSwatch";
         
         new GuiBitmapBorderCtrl() {
            profile = "RTB_ContentBorderProfile";
            horizSizing = "center";
            vertSizing = "center";
            position = "158 109";
            extent = "174 91";
            minExtent = "8 2";
            visible = "0";
            
            dSet = "window";
            dName = "modal_BoxOK";

            new GuiSwatchCtrl() {
               profile = "GuiDefaultProfile";
               horizSizing = "width";
               vertSizing = "height";
               position = "3 3";
               extent = "168 85";
               minExtent = "8 2";
               visible = "1";
               color = "255 255 255 255";

               new GuiSwatchCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "0 0";
                  extent = "168 20";
                  minExtent = "8 2";
                  visible = "1";
                  color = "0 0 0 30";
               };
               new GuiMLTextCtrl() {
                  profile = "GuiMLTextProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "23 5";
                  extent = "140 12";
                  minExtent = "8 2";
                  visible = "1";
                  lineSpacing = "2";
                  allowColorChars = "0";
                  maxChars = "-1";
                  text = "<color:444444><font:Verdana Bold:12>Title Text";
                  maxBitmapHeight = "-1";
                  selectable = "0";
                  
                  dSet = "parent";
                  dName = "m_title";
                  dOffset = 1;
               };
               new GuiBitmapCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "3 2";
                  extent = "16 16";
                  minExtent = "8 2";
                  visible = "1";
                  bitmap = $RTB::Path@"images/icons/information";
                  wrap = "0";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  keepCached = "0";
               };
               new GuiMLTextCtrl() {
                  profile = "GuiMLTextProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "3 23";
                  extent = "162 12";
                  minExtent = "8 2";
                  visible = "1";
                  lineSpacing = "2";
                  allowColorChars = "0";
                  maxChars = "-1";
                  text = "<color:444444><font:Verdana:12>Message Text";
                  maxBitmapHeight = "-1";
                  selectable = "0";
                  
                  dSet = "parent";
                  dName = "m_text";
                  dOffset = 1;
               };
               new GuiBitmapButtonCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "top";
                  position = "147 65";
                  extent = "16 16";
                  minExtent = "8 2";
                  visible = "1";
                  command = %this@".closeModalWindow();";
                  text = " ";
                  groupNum = "-1";
                  buttonType = "PushButton";
                  bitmap = $RTB::Path@"images/ui/buttons/generic/tick";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  mKeepCached = "0";
                  mColor = "255 255 255 255";
                  
                  dSet = "parent";
                  dName = "m_ok";
                  dOffset = 1;
               };
               new GuiTextEditCtrl() {
                  profile = "RTB_TextEditProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "0 -16";
                  extent = "65 16";
                  minExtent = "8 2";
                  visible = "1";
                  altCommand = "eval("@%this@".window.modal_BoxOK.m_ok.command);";
                  maxLength = "32";
                  historySize = "0";
                  password = "1";
                  tabComplete = "0";
                  sinkAllKeyEvents = "0";
                  
                  dSet = "parent";
                  dName = "m_accel";
                  dOffset = 1;
               };
            };
         };
         
         new GuiBitmapBorderCtrl() {
            profile = "RTB_ContentBorderProfile";
            horizSizing = "center";
            vertSizing = "center";
            position = "158 109";
            extent = "174 91";
            minExtent = "8 2";
            visible = "0";
            
            dSet = "window";
            dName = "modal_BoxError";

            new GuiSwatchCtrl() {
               profile = "GuiDefaultProfile";
               horizSizing = "width";
               vertSizing = "height";
               position = "3 3";
               extent = "168 85";
               minExtent = "8 2";
               visible = "1";
               color = "255 255 255 255";

               new GuiSwatchCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "0 0";
                  extent = "168 20";
                  minExtent = "8 2";
                  visible = "1";
                  color = "0 0 0 30";
               };
               new GuiMLTextCtrl() {
                  profile = "GuiMLTextProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "23 5";
                  extent = "140 12";
                  minExtent = "8 2";
                  visible = "1";
                  lineSpacing = "2";
                  allowColorChars = "0";
                  maxChars = "-1";
                  text = "<color:444444><font:Verdana Bold:12>Title Text";
                  maxBitmapHeight = "-1";
                  selectable = "0";
                  
                  dSet = "parent";
                  dName = "m_title";
                  dOffset = 1;
               };
               new GuiBitmapCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "3 2";
                  extent = "16 16";
                  minExtent = "8 2";
                  visible = "1";
                  bitmap = $RTB::Path@"images/icons/exclamation";
                  wrap = "0";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  keepCached = "0";
               };
               new GuiMLTextCtrl() {
                  profile = "GuiMLTextProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "3 23";
                  extent = "162 12";
                  minExtent = "8 2";
                  visible = "1";
                  lineSpacing = "2";
                  allowColorChars = "0";
                  maxChars = "-1";
                  text = "<color:444444><font:Verdana:12>Message Text";
                  maxBitmapHeight = "-1";
                  selectable = "0";
                  
                  dSet = "parent";
                  dName = "m_text";
                  dOffset = 1;
               };
               new GuiBitmapButtonCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "top";
                  position = "147 65";
                  extent = "16 16";
                  minExtent = "8 2";
                  visible = "1";
                  command = %this@".closeModalWindow();";
                  text = " ";
                  groupNum = "-1";
                  buttonType = "PushButton";
                  bitmap = $RTB::Path@"images/ui/buttons/generic/cross";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  mKeepCached = "0";
                  mColor = "255 255 255 255";
                  
                  dSet = "parent";
                  dName = "m_ok";
                  dOffset = 1;
               };
               new GuiTextEditCtrl() {
                  profile = "RTB_TextEditProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "0 -16";
                  extent = "65 16";
                  minExtent = "8 2";
                  visible = "1";
                  altCommand = "eval("@%this@".window.modal_BoxError.m_ok.command);";
                  maxLength = "32";
                  historySize = "0";
                  password = "1";
                  tabComplete = "0";
                  sinkAllKeyEvents = "0";
                  
                  dSet = "parent";
                  dName = "m_accel";
                  dOffset = 1;
               };
            };
         };

         new GuiBitmapBorderCtrl() {
            profile = "RTB_ContentBorderProfile";
            horizSizing = "center";
            vertSizing = "center";
            position = "4 92";
            extent = "144 93";
            minExtent = "8 2";
            visible = "0";
            
            dSet = "window";
            dName = "modal_ChangeRank";

            new GuiSwatchCtrl() {
               profile = "GuiDefaultProfile";
               horizSizing = "width";
               vertSizing = "height";
               position = "3 4";
               extent = "138 87";
               minExtent = "8 2";
               visible = "1";
               color = "255 255 255 255";

               new GuiSwatchCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "top";
                  position = "0 65";
                  extent = "190 20";
                  minExtent = "8 2";
                  visible = "1";
                  color = "0 0 0 30";
               };
               new GuiSwatchCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "0 0";
                  extent = "190 20";
                  minExtent = "8 2";
                  visible = "1";
                  color = "0 0 0 30";
               };
               new GuiMLTextCtrl() {
                  profile = "GuiMLTextProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "23 5";
                  extent = "110 12";
                  minExtent = "8 2";
                  visible = "1";
                  lineSpacing = "2";
                  allowColorChars = "0";
                  maxChars = "-1";
                  text = "<color:444444><font:Verdana Bold:12>Change Rank";
                  maxBitmapHeight = "-1";
                  selectable = "0";
               };
               new GuiBitmapButtonCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "118 3";
                  extent = "16 16";
                  minExtent = "8 2";
                  visible = "1";
                  command = %this@".closeModalWindow();";
                  text = " ";
                  groupNum = "-1";
                  buttonType = "PushButton";
                  bitmap = "Add-Ons/system_returntoblockland/images/ui/buttons/generic/cross";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  mKeepCached = "0";
                  mColor = "255 255 255 255";
               };
               new GuiBitmapCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "3 2";
                  extent = "16 16";
                  minExtent = "8 2";
                  visible = "1";
                  bitmap = "Add-Ons/system_returntoblockland/images/icons/medal_gold_3";
                  wrap = "0";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  keepCached = "0";
               };
               new GuiBitmapBorderCtrl() {
                  profile = "RTB_ContentBorderProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "10 29";
                  extent = "30 30";
                  minExtent = "8 2";
                  visible = "1";

                  new GuiSwatchCtrl() {
                     profile = "GuiDefaultProfile";
                     horizSizing = "width";
                     vertSizing = "height";
                     position = "4 4";
                     extent = "22 22";
                     minExtent = "8 2";
                     visible = "1";
                     color = "255 255 255 255";

                     new GuiBitmapCtrl() {
                        profile = "GuiDefaultProfile";
                        horizSizing = "center";
                        vertSizing = "center";
                        position = "3 3";
                        extent = "16 16";
                        minExtent = "8 2";
                        visible = "1";
                        bitmap = "Add-Ons/System_ReturnToBlockland/images/icons/user";
                        wrap = "0";
                        lockAspectRatio = "0";
                        alignLeft = "0";
                        overflowImage = "0";
                        keepCached = "0";
                     };
                  };
               };
               new GuiBitmapBorderCtrl() {
                  profile = "RTB_ContentBorderProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "54 29";
                  extent = "30 30";
                  minExtent = "8 2";
                  visible = "1";

                  new GuiSwatchCtrl() {
                     profile = "GuiDefaultProfile";
                     horizSizing = "width";
                     vertSizing = "height";
                     position = "4 4";
                     extent = "22 22";
                     minExtent = "8 2";
                     visible = "1";
                     color = "255 255 255 255";

                     new GuiBitmapCtrl() {
                        profile = "GuiDefaultProfile";
                        horizSizing = "center";
                        vertSizing = "center";
                        position = "3 3";
                        extent = "16 16";
                        minExtent = "8 2";
                        visible = "1";
                        bitmap = "Add-Ons/System_ReturnToBlockland/images/icons/shield_silver";
                        wrap = "0";
                        lockAspectRatio = "0";
                        alignLeft = "0";
                        overflowImage = "0";
                        keepCached = "0";
                     };
                  };
               };
               new GuiBitmapBorderCtrl() {
                  profile = "RTB_ContentBorderProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "98 29";
                  extent = "30 30";
                  minExtent = "8 2";
                  visible = "1";

                  new GuiSwatchCtrl() {
                     profile = "GuiDefaultProfile";
                     horizSizing = "width";
                     vertSizing = "height";
                     position = "4 4";
                     extent = "22 22";
                     minExtent = "8 2";
                     visible = "1";
                     color = "255 255 255 255";

                     new GuiBitmapCtrl() {
                        profile = "GuiDefaultProfile";
                        horizSizing = "center";
                        vertSizing = "center";
                        position = "3 3";
                        extent = "16 16";
                        minExtent = "8 2";
                        visible = "1";
                        bitmap = "Add-Ons/System_ReturnToBlockland/images/icons/shield_gold";
                        wrap = "0";
                        lockAspectRatio = "0";
                        alignLeft = "0";
                        overflowImage = "0";
                        keepCached = "0";
                     };
                  };
               };
               new GuiBitmapButtonCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "10 29";
                  extent = "30 30";
                  minExtent = "8 2";
                  visible = "1";
                  command = "";
                  text = " ";
                  groupNum = "-1";
                  buttonType = "PushButton";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  mKeepCached = "0";
                  mColor = "255 255 255 255";
                  
                  dSet = "parent";
                  dName = "btn_normal";
                  dOffset = 1;
               };
               new GuiBitmapButtonCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "54 29";
                  extent = "30 30";
                  minExtent = "8 2";
                  visible = "1";
                  command = "";
                  text = " ";
                  groupNum = "-1";
                  buttonType = "PushButton";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  mKeepCached = "0";
                  mColor = "255 255 255 255";
                  
                  dSet = "parent";
                  dName = "btn_mod";
                  dOffset = 1;
               };
               new GuiBitmapButtonCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "98 29";
                  extent = "30 30";
                  minExtent = "8 2";
                  visible = "1";
                  command = "";
                  text = " ";
                  groupNum = "-1";
                  buttonType = "PushButton";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  mKeepCached = "0";
                  mColor = "255 255 255 255";
                  
                  dSet = "parent";
                  dName = "btn_admin";
                  dOffset = 1;
               };
            };
         };
         new GuiBitmapBorderCtrl() {
            profile = "RTB_ContentBorderProfile";
            horizSizing = "center";
            vertSizing = "center";
            position = "4 92";
            extent = "267 100";
            minExtent = "8 2";
            visible = "0";
            
            dSet = "window";
            dName = "modal_BanUser";

            new GuiSwatchCtrl() {
               profile = "GuiDefaultProfile";
               horizSizing = "width";
               vertSizing = "height";
               position = "3 4";
               extent = "261 94";
               minExtent = "8 2";
               visible = "1";
               color = "255 255 255 255";

               new GuiSwatchCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "width";
                  vertSizing = "top";
                  position = "0 72";
                  extent = "262 20";
                  minExtent = "8 2";
                  visible = "1";
                  color = "0 0 0 30";
               };
               new GuiSwatchCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "width";
                  vertSizing = "bottom";
                  position = "0 0";
                  extent = "261 20";
                  minExtent = "8 2";
                  visible = "1";
                  color = "0 0 0 30";
               };
               new GuiMLTextCtrl() {
                  profile = "GuiMLTextProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "23 5";
                  extent = "110 12";
                  minExtent = "8 2";
                  visible = "1";
                  lineSpacing = "2";
                  allowColorChars = "0";
                  maxChars = "-1";
                  text = "<color:444444><font:Verdana Bold:12>Ban User";
                  maxBitmapHeight = "-1";
                  selectable = "0";
               };
               new GuiBitmapButtonCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "left";
                  vertSizing = "bottom";
                  position = "241 3";
                  extent = "16 16";
                  minExtent = "8 2";
                  visible = "1";
                  command = %this@".closeModalWindow();";
                  text = " ";
                  groupNum = "-1";
                  buttonType = "PushButton";
                  bitmap = "Add-Ons/System_ReturnToBlockland/images/ui/buttons/generic/cross";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  mKeepCached = "0";
                  mColor = "255 255 255 255";
               };
               new GuiBitmapCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "3 2";
                  extent = "16 16";
                  minExtent = "8 2";
                  visible = "1";
                  bitmap = "Add-Ons/System_ReturnToBlockland/images/icons/delete";
                  wrap = "0";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  keepCached = "0";
               };
               new GuiTextEditCtrl() {
                  profile = "RTB_TextEditProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "48 49";
                  extent = "206 16";
                  minExtent = "8 2";
                  visible = "1";
                  maxLength = "255";
                  historySize = "0";
                  password = "0";
                  tabComplete = "0";
                  sinkAllKeyEvents = "0";
                  
                  dSet = "parent";
                  dName = "txt_reason";
                  dOffset = 1;
               };
               new GuiMLTextCtrl() {
                  profile = "GuiMLTextProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "5 50";
                  extent = "42 12";
                  minExtent = "8 2";
                  visible = "1";
                  lineSpacing = "2";
                  allowColorChars = "0";
                  maxChars = "-1";
                  text = "<color:444444><font:Verdana Bold:12>Reason:";
                  maxBitmapHeight = "-1";
                  selectable = "0";
               };
               new GuiMLTextCtrl() {
                  profile = "GuiMLTextProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "5 30";
                  extent = "42 12";
                  minExtent = "8 2";
                  visible = "1";
                  lineSpacing = "2";
                  allowColorChars = "0";
                  maxChars = "-1";
                  text = "<color:444444><font:Verdana Bold:12>Length:";
                  maxBitmapHeight = "-1";
                  selectable = "0";
               };
               new GuiPopUpMenuCtrl() {
                  profile = "RTB_PopupProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "48 28";
                  extent = "139 16";
                  minExtent = "8 2";
                  visible = "1";
                  maxLength = "255";
                  maxPopupHeight = "200";
                  
                  dSet = "parent";
                  dName = "pop_length";
                  dOffset = 1;
               };
               new GuiMLTextCtrl() {
                  profile = "GuiMLTextProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "213 30";
                  extent = "38 12";
                  minExtent = "8 2";
                  visible = "1";
                  lineSpacing = "2";
                  allowColorChars = "0";
                  maxChars = "-1";
                  text = "<color:444444><font:Verdana Bold:12>Forever";
                  maxBitmapHeight = "-1";
                  selectable = "0";
               };
               new GuiCheckBoxCtrl() {
                  profile = "RTB_CheckBoxProfile";
                  horizSizing = "right";
                  vertSizing = "bottom";
                  position = "195 28";
                  extent = "61 18";
                  minExtent = "8 2";
                  visible = "1";
                  text = " ";
                  groupNum = "-1";
                  buttonType = "ToggleButton";
                  
                  dSet = "parent";
                  dName = "chk_forever";
                  dOffset = 1;
               };
               new GuiBitmapButtonCtrl() {
                  profile = "GuiDefaultProfile";
                  horizSizing = "left";
                  vertSizing = "bottom";
                  position = "241 74";
                  extent = "16 16";
                  minExtent = "8 2";
                  visible = "1";
                  text = " ";
                  groupNum = "-1";
                  buttonType = "PushButton";
                  bitmap = $RTB::Path@"images/ui/buttons/generic/tick";
                  lockAspectRatio = "0";
                  alignLeft = "0";
                  overflowImage = "0";
                  mKeepCached = "0";
                  mColor = "255 255 255 255";
                  
                  dSet = "parent";
                  dName = "btn_ban";
                  dOffset = 1;
               };
            };
         };
      };
   };
   RTB_Overlay.add(%window);
   
   %this.window = %window;
   %window.session = %this;
   %this.registerPointers(%window);
 
   %input = RTBCC_InputRecycler.get();
   %window.getObject(5).add(%input);
   %input.horizSizing = "width";
   %input.vertSizing = "bottom";
   %input.position = "1 3";
   %input.extent = "407 16";
   %input.command = %this@".focus();";
   %input.altCommand = %this@".send();";
 
   %window.input = %input;
   %window.closeCommand = %this@".room.leave(1);";
   %window.overlayCloseCommand = %this@".room.leave(0);";
   
   %window.getObject(2).command = %window.userPane@".setVisible(true);"@%window.optsPane@".setVisible(false);";
   %window.getObject(3).command = %window.userPane@".setVisible(false);"@%window.optsPane@".setVisible(true);";
   %window.getObject(2).performClick();
   
   RTBCC_RoomOptionsManager.loadRoomSettings(%this);
   %this.options = RTBCC_RoomOptionsManager.getRoomStore(%this.room.name);
   
   %this.positionWindow();
   
   if(%this.room.conversationHistory !$= "")
   {
      %history = strReplace(strReplace(%this.room.conversationHistory,"\n","<br>"),"<color:","\t");
      %this.room.conversationHistory = "";

      %text = getField(%history,0);
      for(%i=1;%i<getFieldCount(%history);%i++)
      {
         %part = getField(%history,%i);
         %text = %text @ "<color:CCCCCC>" @ getSubStr(%part,7,strLen(%part));
      }
      %window.display.setText("<color:CCCCCC>"@%text@"<color:444444>");
      
      %window.display.setCursorPosition(strLen(%window.display.getValue()));
      %window.scroll.scrollToBottom();
   }
}

//- RTBCC_RoomSession::registerPointers (can't even sum this one up in a line)
function RTBCC_RoomSession::registerPointers(%this, %parent)
{
   for(%i=0;%i<%parent.getCount();%i++)
   {
      %ctrl = %parent.getObject(%i);
      
      if(%ctrl.dSet $= "parent")
      {
         %target = %ctrl.getGroup();
         if(%ctrl.dOffset > 0)
            for(%j=0;%j<%ctrl.dOffset;%j++)
               %target = %target.getGroup();
            
         eval(%target@"."@%ctrl.dName@" = "@%ctrl@";");
      }
      else if(%ctrl.dSet $= "window")
         eval(%this.window@"."@%ctrl.dName@" = "@%ctrl@";");
         
      %ctrl.dSet = "";
      %ctrl.dName = "";
         
      if(%ctrl.getCount() > 0)
         %this.registerPointers(%ctrl);
   }
}

//- RTBCC_RoomSession::positionWindow (determines best position to render room window)
function RTBCC_RoomSession::positionWindow(%this)
{
   if(RTBCO_getPref("CC::SavePositions"))
   {
      if(%this.options.window_position !$= "" && %this.options.window_extent !$= "")
      {
         // some fucking lame torque bug that causes crashes for whatever fucking reason
         if(RTB_Overlay.isAwake())
         {
            %this.window.resize(getWord(%this.options.window_position,0),getWord(%this.options.window_position,1),getWord(%this.options.window_extent,0),getWord(%this.options.window_extent,1));
            %this.positionOnWake = false;
         }
         else
            %this.positionOnWake = true;
         return;
      }
   }
   
   %offset = 0;
   %position = "0 0";
   while(%free !$= true)
   {
      %free = true;
      for(%i=0;%i<RTBCC_RoomSessionManager.getCount();%i++)
      {
         %session = RTBCC_RoomSessionManager.getObject(%i);
         if(!%session.isRendered() || %session $= %this)
            continue;

         if(%session.window.position $= %position)
         {
            %free = false;
            break;
         }
      }
      
      if(%free !$= true)
      {
         %offset += 40;
         %position = %offset SPC %offset;
      }
   }
   %this.window.position = %position;
}

//- RTBCC_RoomSession::isRendered (determines whether the session room is rendered)
function RTBCC_RoomSession::isRendered(%this)
{
   if(isObject(%this.window) && %this.window.session $= %this)
      return true;
   return false;
}

//- RTBCC_RoomSession::unrender (unrenders the session room window)
function RTBCC_RoomSession::unrender(%this)
{
   if(!%this.isRendered())
      return;
      
   %store = RTBCC_RoomOptionsManager.getRoomStore(%this.name);
   %store.window_position = %this.window.position;
   %store.window_extent = %this.window.extent;
   RTBCC_RoomOptionsManager.store();
   
   %this.room.conversationHistory = %this.window.display.getValue();   
   
   RTBCC_InputRecycler.reclaim(%this.window.input);
   %this.window.delete();
   %this.window = "";
}

//- RTBCC_RoomSession::destroy (destroys a room session)
function RTBCC_RoomSession::destroy(%this)
{
   if(isObject(%this.logger))
   {
      %this.logger.close();
      %this.logger = "";
   }
   %this.unrender();
   %this.delete();
}

//*********************************************************
//* Room Session Window Modal Implementation
//*********************************************************
//- RTBCC_RoomSession::setModalWindow (sets modal window for session)
function RTBCC_RoomSession::setModalWindow(%this,%modal)
{
   %group = %this.window.modalSwatch;
   for(%i=0;%i<%group.getCount();%i++)
   {
      %group.getObject(%i).setVisible(false);
   }
   
   if(%modal $= "")
   {
      %group.setVisible(false);
      return;
   }
   %group.setVisible(true);
   
   if(isObject(%this.window.modal_[%modal]))
   {
      %this.window.modal_[%modal].setVisible(true);
      %this.window.modal_[%modal].center();
   }
}

//- RTBCC_RoomSession::closeModalWindow (closes modal window for session)
function RTBCC_RoomSession::closeModalWindow(%this)
{
   %this.window.modalSwatch.setVisible(false);
   
   %this.window.input.makeFirstResponder(1);
}

//- RTBCC_RoomSession::messageBoxOK (opens an ok message box)
function RTBCC_RoomSession::messageBoxOK(%this,%title,%message,%ok)
{
   %this.setModalWindow("BoxOK");
   
   %modal = %this.window.modal_BoxOK;
   %modal.m_title.setText("<color:444444><font:Verdana Bold:12>"@%title);
   %modal.m_text.setText("<color:444444><font:Verdana:12>"@%message);
   %modal.m_ok.command = %this@".closeModalWindow();"@%ok;
   %modal.m_accel.makeFirstResponder(1);
   
   if(RTB_ConnectClient.isOpen())
      %modal.m_text.forceReflow();
   %textHeight = getWord(%modal.m_text.extent,1);
   %modalHeight = %textHeight+52;
   
   if(%modalHeight < 91)
      %modalHeight = 91;
      
   %modal.resize(11,91,174,%modalHeight);
   %modal.center();
}

//- RTBCC_RoomSession::messageBoxError (opens an error message box)
function RTBCC_RoomSession::messageBoxError(%this,%title,%message,%ok)
{
   %this.setModalWindow("BoxError");
   
   %modal = %this.window.modal_BoxError;
   %modal.m_title.setText("<color:444444><font:Verdana Bold:12>"@%title);
   %modal.m_text.setText("<color:444444><font:Verdana:12>"@%message);
   %modal.m_ok.command = %this@".closeModalWindow();"@%ok;
   %modal.m_accel.makeFirstResponder(1);
   
   if(RTB_ConnectClient.isOpen())
      %modal.m_text.forceReflow();
   %textHeight = getWord(%modal.m_text.extent,1);
   %modalHeight = %textHeight+52;
   
   if(%modalHeight < 91)
      %modalHeight = 91;
      
   %modal.resize(11,91,174,%modalHeight);
   %modal.center();
}

//*********************************************************
//* Room Session Manifest Implementation
//*********************************************************
//- RTBCC_RoomSessionManifest::addUser (adds a user to the room session manifest)
function RTBCC_RoomSessionManifest::addUser(%this,%id,%name,%rank,%icon,%blocked)
{
   if(%this.hasUser(%id))
      return %this.getByID(%id);
      
   %user = new ScriptObject()
   {
      class = "RTBCC_RoomSessionManifestUser";
      
      id = %id;
      name = %name;
      rank = %rank;
      icon = %icon;
      
      blocked = %blocked;
      
      session = %this.getGroup();
      manifest = %this;
   };
   %this.add(%user);
   
   if(%this.getCount() == 1)
      %this.getGroup().window.setText(%this.getGroup().window.title @ "   (1 user)");
   else
      %this.getGroup().window.setText(%this.getGroup().window.title @ "   (" @ %this.getCount() @ " users)");
   
   return %user;
}

//- RTBCC_RoomSessionManifest::hasUser (checks if manifest contains user)
function RTBCC_RoomSessionManifest::hasUser(%this,%id)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      if(%this.getObject(%i).id $= %id)
         return true;
   }
   return false;
}

//- RTBCC_RoomSessionManifest::getByID (returns manifest item by id)
function RTBCC_RoomSessionManifest::getByID(%this,%id)
{
   if(!%this.hasUser(%id))
      return false;
      
   for(%i=0;%i<%this.getCount();%i++)
   {
      %user = %this.getObject(%i);
      if(%user.id $= %id)
         return %user;
   }
   return false;
}

//- RTBCC_RoomSessionManifest::removeByID (remove user from session manifest by id)
function RTBCC_RoomSessionManifest::removeByID(%this,%id)
{
   if(!%this.hasUser(%id))
      return false;
      
   %user = %this.getByID(%id);
   %user.unrender();
   %user.delete();
   
   if(%this.getCount() == 1)
      %this.getGroup().window.setText(%this.getGroup().window.title @ "   (1 user)");
   else
      %this.getGroup().window.setText(%this.getGroup().window.title @ "   (" @ %this.getCount() @ " users)");
}

//- RTBCC_RoomSessionManifest::sort (sorts the manifest by rank/name)
function RTBCC_RoomSessionManifest::sort(%this)
{
   if(%this.getCount() <= 0)
      return;
      
   for(%i=3;%i>=0;%i--)
   {
      %sorter = new GuiTextListCtrl();
      for(%j=0;%j<%this.getCount();%j++)
      {
         %user = %this.getObject(%j);
         if(%user.rank $= %i)
            %sorter.addRow(%user,%user.name);
      }
      %sorter.sort(0,1);
      
      for(%j=0;%j<%sorter.rowCount();%j++)
      {
         %this.pushToBack(%sorter.getRowId(%j));
      }
      %sorter.delete();
   }
}

//- RTBCC_RoomSessionManifest::render (renders the manifest)
function RTBCC_RoomSessionManifest::render(%this)
{
   %this.session.window.userSwatch.clear();
   
   %this.sort();
   for(%i=0;%i<%this.getCount();%i++)
   {
      %item = %this.getObject(%i);
      %item.renderInPlace("0" SPC (%i * 22));
   }
   %item.session.window.userSwatch.extent = "0" SPC (%i * 22);
   %item.manifest.reshape();
   
   if(%this.getCount() == 1)
      %this.getGroup().window.setText(%this.getGroup().window.title @ "   (1 user)");
   else
      %this.getGroup().window.setText(%this.getGroup().window.title @ "   (" @ %this.getCount() @ " users)");
}

//- RTBCC_RoomSessionManifest::reshape (reshapes the swatch)
function RTBCC_RoomSessionManifest::reshape(%this)
{
   %swatch = %this.getGroup().window.userSwatch;
   
   %swatch.resize(1,getWord(%swatch.position,1),194,%swatch.getLowestPoint());
}

//*********************************************************
//* Room Session Manifest User Implementation
//*********************************************************
//- RTBCC_RoomSessionManifestUser::isRendered (checks to see if the manifest user is rendered already)
function RTBCC_RoomSessionManifestUser::isRendered(%this)
{
   if(!isObject(%this.gui_container))
      return false;
      
   if(%this.gui_container.getGroup().getID() !$= %this.session.window.userSwatch.getID())
      return false;
      
   return true;
}

//- RTBCC_RoomSessionManifestUser::render (attempts to render the manifest user)
function RTBCC_RoomSessionManifestUser::render(%this)
{
   if(%this.isRendered())
   {
      %this.gui_userIcon.setBitmap($RTB::Path @ "images/icons/" @ %this.icon);
      %this.gui_userName.setText("<color:444444><font:Verdana:12>" @ %this.name);
   }
   else
   {
      %position = 0;
      %this.getGroup().sort();
      for(%i=0;%i<%this.getGroup().getCount();%i++)
      {
         %user = %this.getGroup().getObject(%i);
         if(!%user.isRendered() && %user !$= %this)
            continue;
            
         if(%this $= %user)
         {
            %this.session.window.userSwatch.conditionalShiftY(%position,22);
            %this.renderInPlace("0" SPC %position);
            %this.session.window.userSwatch.extent = vectorAdd(%this.session.window.userSwatch.extent,"0 22");
            %this.manifest.reshape();
         }
         else
            %position += 22;
      }
   }
}

//- RTBCC_RoomSessionManifestUser::renderInPlace (renders a manifest user taking a position argument)
function RTBCC_RoomSessionManifestUser::renderInPlace(%this,%position)
{
   if(%this.isRendered())
      return;
      
   %swatch = %this.session.window.userSwatch;
   
   %container = new GuiSwatchCtrl()
   {
      position = %position;
      extent = "129 22";
      
      color = "0 0 0 0";
   };
   %swatch.add(%container);
   %this.gui_container = %container;
   
   %selectBox = new GuiBitmapCtrl()
   {
      position = "0 0";
      extent = "129 22";
      
      visible = false;
      bitmap = $RTB::Path @ "images/ui/userListSelect_n";
   };
   %container.add(%selectBox);
   %this.gui_selectBox = %selectBox;
   
   %icon = new GuiBitmapCtrl()
   {
      position = "1 3";
      extent = "16 16";
   };
   %container.add(%icon);
   %this.gui_userIcon = %icon;
   
   %text = new GuiMLTextCtrl()
   {
      position = "19 5";
      extent = "147 12";
      
      selectable = false;
   };
   %container.add(%text);
   %this.gui_userName = %text;
   
   %mouseEvent = new GuiMouseEventCtrl()
   {
      position = "0 0";
      extent = "129 22";
      
      persistent = 1;
      eventType = "UserListSelect";
      eventCallbacks = "1111011";
      
      user = %this;
      select = %selectBox;
      session = %this.getGroup().session;
   };
   %container.add(%mouseEvent);
   %this.gui_mouseEvent = %mouseEvent;
   
   %this.render();
}

//- RTBCC_RoomSessionManifestUser::rerender (rerenders the manifest user if it's already rendered)
function RTBCC_RoomSessionManifestUser::rerender(%this)
{
   if(%this.isRendered())
      %this.unrender();
   %this.render();
}

//- RTBCC_RoomSessionManifestUser::unrender (unrenders the manifest user)
function RTBCC_RoomSessionManifestUser::unrender(%this)
{
   if(!%this.isRendered())
      return;
      
   %this.closeMenu();
   %position = getWord(%this.gui_container.position,1);

   %this.gui_container.delete();

   %this.session.window.userSwatch.conditionalShiftY(%position,-22);
   %this.session.window.userSwatch.extent = vectorSub(%this.session.window.userSwatch.extent,"0 22");
   
   %this.manifest.reshape();
}

//- RTBCC_RoomSessionManifestUser::openMenu (opens a menu of items for the user to select)
function RTBCC_RoomSessionManifestUser::openMenu(%this)
{
   %this.gui_selectBox.setBitmap($RTB::Path @ "images/ui/userListSelect_d");
   
   %top = getWord(%this.gui_selectBox.getPosRelativeTo(%this.session.window.userScroll),1);
   %bottom = %top + getWord(%this.gui_selectBox.extent,1);
   %scrollExt = getWord(%this.session.window.userScroll.extent,1)-2;

   if(%top < 0)
      %this.session.window.userSwatch.resize(1,getWord(%this.session.window.userSwatch.position,1)-(%top-2),140,getWord(%this.session.window.userSwatch.extent,1));
   if(%bottom > %scrollExt)
      %this.session.window.userSwatch.resize(1,getWord(%this.session.window.userSwatch.position,1)-(%bottom-%scrollExt)-2,140,getWord(%this.session.window.userSwatch.extent,1));
      
   
   %menuItems = -1;
   %menuIcon[%menuItems++] = "comment";
   %menuText[%menuItems] = "Chat";
   %menuComm[%menuItems] = %this@".openChatWindow();";
   %menuIcon[%menuItems++] = "information";
   %menuText[%menuItems] = "Info";
   %menuComm[%menuItems] = %this@".info();";
   if(!RTBCC_Roster.hasID(%this.id) && !RTBCC_InviteRoster.hasID(%this.id))
   {
      %menuIcon[%menuItems++] = "heart_add";
      %menuText[%menuItems] = "Friend";
      %menuComm[%menuItems] = %this@".addFriend();";
   }
   if(%this.manifest.getById(RTB_ConnectClient.client_id).rank >= 1 && %this.rank < %this.manifest.getById(RTB_ConnectClient.client_id).rank)
   {
      %menuIcon[%menuItems++] = "block";
      %menuText[%menuItems] = "Kick";
      %menuComm[%menuItems] = %this@".kick();";
   }
   if(%this.manifest.getById(RTB_ConnectClient.client_id).rank >= 2 && %this.rank < %this.manifest.getById(RTB_ConnectClient.client_id).rank)
   {
      %menuIcon[%menuItems++] = "delete";
      %menuText[%menuItems] = "Ban";
      %menuComm[%menuItems] = %this@".ban();";
      
      %menuIcon[%menuItems++] = "medal_gold_3";
      %menuText[%menuItems] = "Rank";
      %menuComm[%menuItems] = %this@".changeRank();";
   }
   %menuItems++;
   
   %menuSize = (%menuItems * 20) + 4;
   
   %container = RTB_Overlay;
   %position = %this.gui_selectBox.getAbsPosition(%container);
   %menu = new GuiSwatchCtrl()
   {
      position = vectorAdd(%position,"63 22");
      extent = "66" SPC %menuSize;
      color = "0 0 0 0";
      
      user = %this;
      
      new GuiBitmapCtrl()
      {
         position = "0" SPC %menuSize - 4;
         extent = "66 4";
         
         bitmap = $RTB::Path @ "images/ui/buddyListMenuBottom";
      };
   };
   %container.add(%menu);
   %this.gui_menu = %menu;
   %this.session.gui_userMenu = %menu;
   
   for(%i=0;%i<%menuItems;%i++)
   {
      %item = new GuiBitmapCtrl()
      {
         position = "0" SPC (%i * 20);
         extent = "66 20";
         
         bitmap = $RTB::Path @ "images/ui/buddyListMenu_n";
         
         new GuiBitmapCtrl()
         {
            position = "4 2";
            extent = "16 16";
            
            bitmap = $RTB::Path @ "images/icons/" @ %menuIcon[%i];
         };
         
         new GuiMLTextCtrl()
         {
            position = "22 4";
            extent = "54 12";
            
            text = "<color:444444><font:Verdana:12>" @ %menuText[%i];
            
            selectable = false;
         };
      };
      %menu.add(%item);
      
      %mouseEvent = new GuiMouseEventCtrl()
      {
         position = "0" SPC (%i * 20);
         extent = "66 20";
         
         eventType = "UserListMenu";
         eventCallbacks = "1101000";
         
         user = %this;
         item = %item;
         command = %menuComm[%i];
      };
      %menu.add(%mouseEvent);
   }
   %this.gui_mouseEvent.extent = "129" SPC (%menuSize + 30);
   
   if(RTBCO_getPref("CC::EnableSounds"))
      alxPlay(RTBCC_TickSound);
}

//- RTBCC_RoomSessionManifestUser::closeMenu (closes the roster menu)
function RTBCC_RoomSessionManifestUser::closeMenu(%this)
{
   if(isObject(%this.gui_menu))
   {
      %this.session.gui_userMenu = "";
      %this.gui_menu.schedule(1,"delete");
      if(RTBCO_getPref("CC::EnableSounds"))
         alxPlay(RTBCC_TickSound);
   }
   
   %this.gui_mouseEvent.extent = "129 22";
   %this.gui_selectBox.setBitmap($RTB::Path @ "images/ui/userListSelect_n");
}

//- Event_UserListSelect::onMouseEnter (handles entry interaction with roster user item)
function Event_UserListSelect::onMouseEnter(%this)
{
   %this.select.setVisible(true);
}

//- Event_UserListSelect::onMouseLeave (handles leaving interaction with roster user item)
function Event_UserListSelect::onMouseLeave(%this)
{     
   %this.select.setVisible(false);
   
   %this.user.closeMenu();
}

//- Event_UserListSelect::onMouseDown (handles click interaction with roster user item)
function Event_UserListSelect::onMouseDown(%this)
{
   if(%this.user.id $= RTB_ConnectClient.client_id)
      return;
      
   if(isObject(%this.session.gui_userMenu) && %this.session.gui_userMenu.user !$= %this.user)
      %this.session.gui_userMenu.user.closeMenu();
      
   if(isObject(%this.user.gui_menu))
      return;
      
   %this.select.setBitmap($RTB::Path @ "images/ui/userListSelect_h");
}

//- Event_UserListSelect::onMouseUp (handles click interaction with roster user item)
function Event_UserListSelect::onMouseUp(%this)
{
   if(%this.user.id $= RTB_ConnectClient.client_id)
      return;
      
   if(isObject(%this.session.gui_userMenu) && %this.session.gui_userMenu.user !$= %this.user)
      return;
      
   if(isObject(%this.user.gui_menu))
   {
      %this.user.closeMenu();
      
      if((getSimTime() - %this.lastClickTime) <= 300)
         %this.user.openChatWindow();
         
      return;
   }
      
   %this.user.openMenu();
   
   %this.lastClickTime = getSimTime();
}

//- Event_UserListSelect::onRightMouseDown (handles click interaction with roster user item)
function Event_UserListSelect::onRightMouseDown(%this)
{
   if(%this.user.id $= RTB_ConnectClient.client_id)
      return;
      
   if(isObject(%this.session.gui_userMenu) && %this.session.gui_userMenu.user !$= %this.user)
      %this.session.gui_userMenu.user.closeMenu();
      
   if(isObject(%this.user.gui_menu))
      return;
      
   %this.select.setBitmap($RTB::Path @ "images/ui/userListSelect_h");
}

//- Event_UserListSelect::onRightMouseUp (handles click interaction with roster user item)
function Event_UserListSelect::onRightMouseUp(%this)
{
   if(%this.user.id $= RTB_ConnectClient.client_id)
      return;
      
   if(isObject(%this.session.gui_userMenu) && %this.session.gui_userMenu.user !$= %this.user)
      return;
      
   if(isObject(%this.user.gui_menu))
   {
      %this.user.closeMenu();
      return;
   }
      
   %this.user.openMenu();
   
   %this.lastClickTime = getSimTime();
}

//- Event_BuddyListMenu::onMouseEnter (handles menu item interaction of the roster interact menu)
function Event_UserListMenu::onMouseEnter(%this)
{
   %this.item.setBitmap($RTB::Path @ "images/ui/buddyListMenu_h");
}

//- Event_BuddyListMenu::onMouseLeave (handles menu item interaction of the roster interact menu)
function Event_UserListMenu::onMouseLeave(%this)
{
   %this.item.setBitmap($RTB::Path @ "images/ui/buddyListMenu_n");
}

//- Event_BuddyListMenu::onMouseUp (handles menu item interaction of the roster interact menu)
function Event_UserListMenu::onMouseUp(%this)
{
   eval(%this.command);
}

//- RTBCC_RoomSessionManifestUser::openChatWindow (opens chat window with user)
function RTBCC_RoomSessionManifestUser::openChatWindow(%this)
{
   if(!RTBCC_Roster.hasID(%this.id))
      RTBCC_TempRoster.addUser(%this.id,%this.name);
      
   if(RTBCC_SessionManager.hasID(%this.id))
      RTBCC_SessionManager.getByID(%this.id).render();
   else
      RTBCC_SessionManager.createSession(%this.id);
      
   RTBCC_SessionManager.getByID(%this.id).setBlockedStatus(%this.blocked);
   
   %this.closeMenu();
}

//- RTBCC_RoomSessionManifestUser::info (gets info on a user)
function RTBCC_RoomSessionManifestUser::info(%this)
{
   RTBCC_Socket.getUserInfo(%this.id);
   RTB_ConnectClient.messageBox("Please Wait ...","Getting user details for Blockland ID "@%this.id);
   
   %this.closeMenu();
}

//- RTBCC_RoomSessionManifestUser::addFriend (adds user as a friend)
function RTBCC_RoomSessionManifestUser::addFriend(%this)
{
   RTBCC_Socket.addToRoster(%this.id,%this.session);
   
   %this.closeMenu();
}

//- RTBCC_RoomSessionManifestUser::kick (tries to kick a user)
function RTBCC_RoomSessionManifestUser::kick(%this)
{
   RTBCC_Socket.kickUser(%this.session.name,%this.id);
   
   %this.closeMenu();
}

//- RTBCC_RoomSessionManifestUser::ban (tries to ban a user)
function RTBCC_RoomSessionManifestUser::ban(%this,%flag)
{  
   %this.closeMenu();

   %modal = %this.session.window.modal_BanUser;   
   
   if(%flag $= "")
   {
      %this.session.setModalWindow("BanUser");
      
      %modal.btn_ban.command = %this@".ban(1);";
      
      %select = %modal.pop_length;
      %select.clear();
      
      %select.add("5 Minutes",60*5);
      %select.add("15 Minutes",60*15);
      %select.add("30 Minutes",60*30);
      %select.add("1 Hour",60*60);
      %select.add("2 Hours",60*60*2);
      %select.add("6 Hours",60*60*6);
      %select.add("12 Hours",60*60*12);
      %select.add("1 Day",60*60*24);
      %select.add("2 Days",60*60*24*2);
      %select.add("5 Days",60*60*24*5);
      %select.add("1 Week",60*60*24*7);
      
      return;
   }
   %this.session.closeModalWindow();
   
   %reason = %modal.txt_reason;
   %length = (%modal.chk_forever.getValue() $= 1) ? -1 : %modal.pop_length.getSelected();
   
   RTBCC_Socket.banUser(%this.session.name,%this.id,%reason,%length);
}

//- RTBCC_RoomSessionManifestUser::changeRank (opens dialog to change rank, and changes it)
function RTBCC_RoomSessionManifestUser::changeRank(%this,%rank)
{
   %this.closeMenu();
   
   if(%rank $= "")
   {
      %this.session.setModalWindow("ChangeRank");
      
      %modal = %this.session.window.modal_ChangeRank;
      %modal.btn_normal.command = %this@".changeRank(0);";
      %modal.btn_mod.command = %this@".changeRank(1);";
      %modal.btn_admin.command = %this@".changeRank(2);";
      
      return;
   }
   %this.session.closeModalWindow();
   
   RTBCC_Socket.changeUserRank(%this.session.name,%this.id,%rank);
}

//*********************************************************
//* Room Options Manager
//*********************************************************
//- RTBCC_createRoomOptionsManager (creates a room options manager)
function RTBCC_createRoomOptionsManager()
{
   if(isObject(RTBCC_RoomOptionsManager))
      RTBCC_RoomOptionsManager.delete();

   if(isFile("config/client/rtb/rooms.cs"))
      exec("config/client/rtb/rooms.cs");
      
   if(isObject(RTBCC_RoomOptionsManager))
   {
      RTBGroup.add(RTBCC_RoomOptionsManager);
      return RTBCC_RoomOptionsManager;
   }
      
   %manager = new ScriptGroup(RTBCC_RoomOptionsManager)
   {
      new ScriptObject()
      {
         room = "General Discussion";
         autojoin = 1;
      };
   };
   RTBGroup.add(%manager);
   
   return %manager;
}

//- RTBCC_RoomOptionsManager::store (saves settings to file)
function RTBCC_RoomOptionsManager::store(%this)
{
   %this.save("config/client/rtb/rooms.cs");
}

//- RTBCC_RoomOptionsManager::storeRoomSettings (stores settings for a specific room)
function RTBCC_RoomOptionsManager::storeRoomSettings(%this,%session)
{
   %roomStore = %this.getRoomStore(%session.room.name);

   %swatch = %session.window.optsSwatch;
   for(%i=0;%i<%swatch.getCount();%i++)
   {
      %ctrl = %swatch.getObject(%i);
      if(%ctrl.getClassName() $= "GuiCheckboxCtrl" && %ctrl.var !$= "")
         eval(%roomStore@"."@%ctrl.var@" = "@%ctrl.getValue()@";");
   }
   %this.store();
}

//- RTBCC_RoomOptionsManager::loadRoomSettings (loads settings for a specific room)
function RTBCC_RoomOptionsManager::loadRoomSettings(%this,%session)
{
   %roomStore = %this.getRoomStore(%session.room.name);

   %swatch = %session.window.optsSwatch;
   for(%i=0;%i<%swatch.getCount();%i++)
   {
      %ctrl = %swatch.getObject(%i);
      if(%ctrl.getClassName() $= "GuiCheckboxCtrl" && %ctrl.var !$= "")
      {
         eval("%value = "@%roomStore@"."@%ctrl.var@";");
         if(%value !$= "")
            %ctrl.setValue(%value);
         else
            %ctrl.setValue(%ctrl.def);
      }
   }
   %this.store();
}

//- RTBCC_RoomOptionsManager::hasRoomStore (checks to see if store for room exists)
function RTBCC_RoomOptionsManager::hasRoomStore(%this,%name)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      if(%this.getObject(%i).room $= %name)
         return true;
   }
   return false;
}

//- RTBCC_RoomOptionsManager::getRoomStore (gets an object to store settings for a room)
function RTBCC_RoomOptionsManager::getRoomStore(%this,%name)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      if(%this.getObject(%i).room $= %name)
         return %this.getObject(%i);
   }
   
   %store = new ScriptObject()
   {
      room = %name;
   };
   %this.add(%store);
   
   return %store;
}

//*********************************************************
//* GuiInputCtrl Recycling
//* ----------------------
//* There's a torque memory corruption bug relating to the
//* dynamic creation and destruction of GuiInputCtrl
//* objects which causes crashes when they're used so we're
//* going to create a bunch and recycle them.
//*********************************************************
//- RTBCC_createInputRecycler (creates an input recycler)
function RTBCC_createInputRecycler()
{
   if(isObject(RTBCC_InputRecycler))
   {
      echo("\c2ERROR: Cannot destroy input recycler!");
      return RTBCC_InputRecycler;
   }
   
   %recycler = new GuiSwatchCtrl(RTBCC_InputRecycler)
   {
      visible = false;
   };
   RTB_ConnectClient.add(%recycler);
   
   %recycler.populate(50);
   
   return %recycler;
}

//- RTBCC_InputRecycler::get (returns a free input object)
function RTBCC_InputRecycler::get(%this)
{
   if(%this.getCount() >= 1)
   {
      %input = %this.getObject(0);
      %input.setValue("");
      return %input;
   }
   else
   {
      echo("\c2ERROR: Unable to allocate free input object!");
      return false;
   }
}

//- RTBCC_InputRecycler::reclaim (reclaims an input object)
function RTBCC_InputRecycler::reclaim(%this,%input)
{
   if(!isObject(%input))
      return false;
   
   %input.setValue("");
   %input.command = "";
   %input.altCommand = "";
      
   %this.add(%input);
   
   return true;
}

//- RTBCC_InputRecycler::populate (populates recycler)
function RTBCC_InputRecycler::populate(%this,%amount)
{
   if(%this.getCount() > 0)
   {
      echo("\c2ERROR: Cannot re-populate input recycler!");
      return false;
   }
   
   for(%i=0;%i<%amount;%i++)
   {
      %input = new GuiTextEditCtrl()
      {
         profile = RTB_TextEditProfile;
         horizSizing = "right";
         vertSizing = "bottom";
         position = "0 0";
         extent = "64 16";
         maxLength = "255";
         command = "";
         altCommand = "";
         accelerator = "return";
         historySize = 32;
      };
      %this.add(%input);
   }
   return (%this.getCount() == %amount);
}

//*********************************************************
//* Notification Manager
//*********************************************************
//- RTBCC_createNotificationManager (creates a notification manager)
function RTBCC_createNotificationManager()
{
   if(isObject(RTBCC_NotificationManager))
   {
      RTBCC_NotificationManager.destroy();
      RTBCC_NotificationManager.delete();
   }
   
   %manager = new ScriptGroup(RTBCC_NotificationManager);
   RTBGroup.add(%manager);
   
   return %manager;
}

//- RTBCC_NotificationManager::push (pushes a notification)
function RTBCC_NotificationManager::push(%this,%title,%message,%icon,%key,%holdTime)
{
   if(RTB_Overlay.isAwake())
      return;
      
   if(%key !$= "")
   {
      for(%i=0;%i<%this.getCount();%i++)
      {
         %notification = %this.getObject(%i);
         if(%notification.key $= %key)
         {
            %notification.icon = %icon;
            %notification.title = %title;
            %notification.message = %message;
            %notification.render();
            return;
         }
      }
   }
   
   if(%icon $= "")
      %icon = "information";
      
   if(%holdTime $= "")
      %holdTime = 3000;
      
   if(%holdTime < 0 && !RTBCO_getPref("CC::StickyNotifications"))
      %holdTime = 5000;
   
   %notification = new ScriptObject()
   {
      class = "RTBCC_Notification";
      
      key = %key;
      icon = %icon;
      title = %title;
      message = %message;
      
      holdTime = %holdTime;
      
      state = "left";
   };
   %this.add(%notification);
   
   %notification.render();
}

//- RTBCC_NotificationManager::pop (removes a notification by key)
function RTBCC_NotificationManager::pop(%this,%key)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %notification = %this.getObject(%i);
      if(%notification.key $= %key)
      {
         %notification.state = "right";
         %notification.step();
         break;
      }
   }
}

//- RTBCC_NotificationManager::refocus (moves all notifications to current window)
function RTBCC_NotificationManager::refocus(%this)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %notification = %this.getObject(%i);
      if(isObject(%notification.canvas) && %notification.canvas.script $= %notification)
         Canvas.getObject(Canvas.getCount()-1).add(%notification.canvas);
   }
}

//- RTBCC_NotificationManager::destroy (destroys all traces of the object)
function RTBCC_NotificationManager::destroy(%this)
{
   for(%i=0;%i<%this.getCount();%i++)
   {
      %notification = %this.getObject(%i);
      if(isObject(%notification.canvas) && %notification.canvas.script $= %notification)
      {
         cancel(%notification.moveAnim);
         %notification.canvas.delete();
      }
   }
   %this.clear();
}

//- RTBCC_Notification::render (handles the rendering/drawing of a notification)
function RTBCC_Notification::render(%this)
{
   %width = 209;
   %height = 50;
   
   if(isObject(%this.canvas) && %this.canvas.script $= %this)
   {
      %this.setIcon(%this.icon);
      %this.setTitle(%this.title);
      %this.setMessage(%this.message);
      
      cancel(%this.moveAnim);
      %this.state = "left";
      
      %this.step();
   }
   else
   {
      %xPosition = getWord(getRes(),0) - %width;
      %yPosition = getWord(getRes(),1) - %height;
      
      %manager = %this.getGroup();
      for(%i=0;%i<%manager.getCount();%i++)
      {
         %notification = %manager.getObject(%i);
         if(isObject(%notification.canvas) && %notification.canvas.script $= %notification)
         {
            if(getWord(%notification.canvas.position,1) <= %yPosition)
               %yPosition = getWord(%notification.canvas.position,1)-%height;
         }
      }
      
      if(%yPosition < 0)
         return;
      
      %canvas = new GuiSwatchCtrl()
      {
         position = %xPosition SPC %yPosition;
         extent = %width SPC %height;
         color = "0 0 0 0";
         
         script = %this;
      };
      Canvas.getObject(canvas.getCount()-1).add(%canvas);
      
      %this.canvas = %canvas;
      
      if(canvas.getContent().getName() $= "PlayGui")
         %this.drawPlay();
      else
         %this.drawMenu();
         
      %this.step();
   }
}

//- RTBCC_Notification::step (plays a step through the animation)
function RTBCC_Notification::step(%this)
{
   if(%this.state $= "left")
   {
      if(getWord(%this.window.position,0) <= 0)
      {
         if(%this.holdTime < 0)
         {
            %this.window.position = "0 0";
            %this.state = "done";
            return;
         }
         %this.window.position = "0 0";
         %this.state = "wait";
         %this.moveAnim = %this.schedule(%this.holdTime,"step");
         return;
      }
      %this.window.position = vectorSub(%this.window.position,"10 0");
      %this.moveAnim = %this.schedule(10,"step");
   }
   else if(%this.state $= "wait")
   {
      %this.state = "right";
      %this.step();
   }
   else if(%this.state $= "right")
   {
      if(getWord(%this.window.position,0) >= getWord(%this.canvas.extent,0))
      {
         %this.window.position = getWord(%this.canvas.extent,0) SPC "0";
         %this.state = "done";
         %this.step();
         return;
      }
      %this.window.position = vectorAdd(%this.window.position,"10 0");
      %this.moveAnim = %this.schedule(10,"step");
   }
   else if(%this.state $= "done")
   {
      %y = getWord(%this.canvas.position,1);
      %this.canvas.delete();

      for(%i=0;%i<RTBCC_NotificationManager.getCount();%i++)
      {
         %notification = RTBCC_NotificationManager.getObject(%i);
         if(!isObject(%notification.canvas))
            continue;
         if(getWord(%notification.canvas.position,1) < %y)
            %notification.canvas.shift(0,50);
      }
      %this.delete();
   }
}

//- RTBCC_Notification::drawPlay (draws the PlayGui version of the notification)
function RTBCC_Notification::drawPlay(%this)
{
   %draw = new GuiBitmapCtrl() {
      position = getWord(%this.canvas.extent,0) SPC "0";
      extent = "204 44";
      bitmap = $RTB::Path@"images/ui/notificationPlay";
      
      new GuiBitmapCtrl() {
         position = "13 12";
         extent = "16 16";
         bitmap = $RTB::Path@"images/icons/"@%this.icon;
      };
      new GuiMLTextCtrl() {
         position = "41 5";
         extent = "154 14"; 
         text = "<shadow:2:2><shadowcolor:00000066><color:EEEEEE><font:Verdana Bold:15>"@%this.title;
         selectable = false;
      };
      new GuiMLTextCtrl() {
         position = "41 21";
         extent = "165 12";
         text = "<shadow:2:2><shadowcolor:00000066><color:DDDDDD><font:Verdana:12>"@%this.message;
         selectable = false;
      };
      new GuiBitmapButtonCtrl() {
         position = "0 0";
         extent = "204 44";
         text = " ";
         command = "RTB_Overlay.fadeIn();";
      };
   };
   %this.canvas.add(%draw);

   %this.window = %draw;
}

//- RTBCC_Notification::drawMenu (draws the menu version of the notification)
function RTBCC_Notification::drawMenu(%this)
{
   %draw = new GuiBitmapCtrl() {
      position = getWord(%this.canvas.extent,0) SPC "0";
      extent = "204 44";
      bitmap = $RTB::Path@"images/ui/notificationMenu";
      
      new GuiBitmapCtrl() {
         position = "13 12";
         extent = "16 16";
         bitmap = $RTB::Path@"images/icons/"@%this.icon;
      };
      new GuiMLTextCtrl() {
         position = "41 5";
         extent = "154 14"; 
         text = "<shadow:2:2><shadowcolor:00000066><color:EEEEEE><font:Verdana Bold:15>"@%this.title;
         selectable = false;
      };
      new GuiMLTextCtrl() {
         position = "41 21";
         extent = "165 12";
         text = "<shadow:2:2><shadowcolor:00000066><color:DDDDDD><font:Verdana:12>"@%this.message;
         selectable = false;
      };
      new GuiBitmapButtonCtrl() {
         position = "0 0";
         extent = "204 44";
         text = " ";
         command = "RTB_Overlay.fadeIn();";
      };
   };
   %this.canvas.add(%draw);

   %this.window = %draw;
}

//- RTBCC_Notification::setIcon (sets the notification icon)
function RTBCC_Notification::setIcon(%this,%icon)
{
   %this.window.getObject(0).setBitmap($RTB::Path@"images/icons/"@%icon);
}

//- RTBCC_Notification::setTitle (sets the notification title)
function RTBCC_Notification::setTitle(%this,%title)
{
   %this.window.getObject(1).setText("<shadow:2:2><shadowcolor:00000066><color:EEEEEE><font:Verdana Bold:15>"@%title);
}

//- RTBCC_Notification::setMessage (sets the notification message)
function RTBCC_Notification::setMessage(%this,%message)
{
   %this.window.getObject(2).setText("<shadow:2:2><shadowcolor:00000066><color:DDDDDD><font:Verdana:12>"@%message);
}

//*********************************************************
//* Connect Packaging
//*********************************************************
package RTB_Modules_Client_ConnectClient
{   
   function GameConnection::onConnectionAccepted(%this,%a,%b,%c,%d,%e,%f,%g,%h,%i,%j,%k)
   {
      Parent::onConnectionAccepted(%this,%a,%b,%c,%d,%e,%f,%g,%h,%i,%j,%k);
      
      if(RTBCC_Socket.authenticated)
         RTBCC_Socket.sendPresence();
   }
   
   function connectingGui::cancel(%this)
   {
      Parent::cancel(%this);
      
      if(RTBCC_Socket.authenticated)
         RTBCC_Socket.sendPresence();
   }
   
   function disconnectedCleanup()
   {
      Parent::disconnectedCleanup();
      
      if(RTBCC_Socket.authenticated)
         RTBCC_Socket.sendPresence(0,1);
   }
   
   function Canvas::setContent(%this,%content)
   {
      if(Canvas.getContent() $= LoadingGui.getId() && %content.getId() $= PlayGui.getId() && RTB_Overlay.isAwake())
         %reopenOverlay = true;
      
      Parent::setContent(%this,%content);
      
      if(isObject(RTBCC_NotificationManager) && %content !$= "noHudGui")
         RTBCC_NotificationManager.refocus();
         
      if(%reopenOverlay)
         Canvas.pushDialog(RTB_Overlay);
   }
   
   function Canvas::pushDialog(%this,%dialog)
   {
      Parent::pushDialog(%this,%dialog);
      
      if(isObject(RTBCC_NotificationManager))
         if(%dialog $= RTB_Overlay)
            RTBCC_NotificationManager.destroy();
         else
            RTBCC_NotificationManager.refocus();
   }
   
   function Canvas::popDialog(%this,%dialog)
   {
      Parent::popDialog(%this,%dialog);
      
      if(isObject(RTBCC_NotificationManager))
         RTBCC_NotificationManager.refocus();
   }
   
   function Avatar_Done()
   {
      Parent::Avatar_Done();
      
      RTB_ConnectClient.setAvatar();
   }
   
   function MM_AuthBar::blinkSuccess(%this)
   {
      Parent::blinkSuccess(%this);
      
      RTB_ConnectClient.setDetails();
      
      if(RTBCO_getPref("CC::AutoSignIn") && !RTBCC_Socket.connected)
         RTBCC_Socket.connect();
   }
   
   function keyGui::done()
   {
      Parent::done();
      
      if(RTBCC_Socket.connected)
         RTBCC_Socket.softDisconnect();
   }
};
RTB_ConnectClient.init();

RTBCC_NotificationManager.schedule(1000,"push","Press \""@ getField(RTBCO_getPref("OV::OverlayKeybind"),1) @"\"","to open the RTB Overlay.","","",5000);