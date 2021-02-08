Project started on November 11, 2019 
University of Utah CS 3500

Gavin Gray: u1040250
Dan Ruley: u0956834

NAMESPACES:
	TankWars:
		The "model" is contained within the TankWars namespace.  This includes two major projects that make up the game model as well as the provided Vector2D helper class:
		You will find all GAMEOBJECTS inside the model, as well as the Vector2D project which contains the important Vector2D class.

	ViewController:  
			This namespace contains the TankWarsViewController class.

	TankWarsView:
		Contains the TankWarsView class that represents the "view" for the TankWars game, as well as the DrawingPanel class which is used for drawing the world and game objects.

TankWarsViewController:
	Responsible for all communication between the client and server, and firing events that notify the view if it needs to update or in the case of an error.

GameWorld project:
	Contains the GameWorld class which is mostly a container class for all of the GameObjects.  It holds these objects in Dictionaries to
	provide O(1) access, insertions, and deletions of these objects.

GameObjects project:
	Instead of making a seperate project for every game object, we decided it would be clearer to put them all in the same project, in seperate files. 
	Each file is named acoordingly <class name>.cs where all objects fall into the same namespace. Therefore any project which needs to declare a game 
	object will need to reference the GameObjects project and have a using directive to the GameObjects namespace. 

	Additionally for all the classes, we have made all the properties public, with private 'setters'. This allows them to still be deserialized from JSON 
	objects and all projects that need the information is able to use the public 'getter'. 

	Wall.cs:
		This object is slightly more interesting than the others because of how walls are passed. To draw the walls we are using 50x50 images placed next to each
		other in order to form the whole wall. To quickly get the coordinates of where each image needs to draw we have created a helper mether method to do so.
		IEnumerable<Vector2D> GetAllCoordinates(double width) is the function signature. What is returned is an IEnumerable<Vector2D> which is where all the wall
		images need to be drawn in order to make the complete wall. The method takes in a double, which represents the width that each wall image should be drawn at,
		this parameter can be changed as needed, however, for our purposes it is 50. 

	Explosion.cs:
		In order to draw explosions we decided it would be more fun to use an animated GIF rather than just drawing some lame shapes like Danny did in the sample client. 
		How is works is that an Explosion object is initialized with an Image, an id (the id of the tank that died), and a location (the location of the tank that died). 
		In order for us to maintain the Controller NOT having a reference to the DrawingPanel we made this triggered by an event which is then invoked in the view. The view, 
		then calls a public method in the DrawingPanel that will create a new explosion at the location, with the pre-loaded GIF. The DrawingPanel has to create the 
		Explosion because it is the only location which has the Image object ready. However, the image is Cloned using the ICloneable interface before it is passed in in 
		order for us to allow the possibility of multiple explosion on the screen at a time.  Then whenever we need to draw the next frame of the explosion the DrawNextFrame() 
		method is invoked which returns the image to be drawn at that specific frame. When all the frames of the GIF have been run it will return NULL which indicates to the 
		DrawingPanel that it needs to remove the explosion from the world. 

	Vector2D:
		The operators < and > have been overrided in order to more easily compare two points. This has been done in order to have the capability of drawing diagonal walls.
		NOTE** Drawing diagonal walls IS NOT CURRENTLY IMPLEMENTED, therefore, do not try to draw any diagonal walls.

Player input/handling KeyDown, KeyUp events:
	When a KeyBoard event happens the view sends the string version of the pressed key to the controller. If the string is either "A","S","D","W" the player code is moved 
	acoordingly. 
	How does this happen exactly. When a KeyDown event is fired, the string version of the key is put into a List of UNIQUE strings. However, strings are only saved in
	the list if they have a special meaning, i.g. the four keys for moving a tank. When a KeyUp up event happens the string of the key is REMOVED from the list. When a 
	command is sent to the server the last string in the list is sent, or to be more precise, the last key that was pressed down and not released. 
	While this is not the most efficient way of storing the key presses, it is sufficient for our needs because the amount of items in the list will NEVER exceed 4. Which
	essentially keeps the time complexity to constant time. 

DrawingPanel:
	Drawing game objects:
		The drawing panel has a reference to the Game World and can access the various Game Objects via this reference.  The OnPaint method is called from the GameView form 
		class, triggered by a GameController event which indicates that all server data has been processed for thisframe, and TheWorld is ready to be redrawn.
		The drawing panel contains code provided in Lab11 for drawing images with tranforms to draw them correctly with regards to the relative sizes of the drawing panel 
		and the world size.  It also contains the code from the PS8 specs for centering the panel on the player tank.
		There are many delegate helper methods passed to DrawObjectWithTranform that handle the drawing of all the various game objects.




######################################################################################  PS9   ################################################################################################

Changes From PS8:
	Note To Graders** We know that in class Professor Kopta said that only the demo client would be used with our server. If it is the case that our Client is used with the server
	please don't use the one from PS8, we have included a few special things that make our new Client much more compatable with a dynamic server. For example, the Starting HP can be changed,
	and the new Client will scale the HP bar as necessary. 

Server Overview:
	When designing the server we opted for a sort of 'processor' design because essentially the clients are giving it 'commands', the server processes them in order, then compues the 
	next frame.
	Additionally, when a player gets a powerup, allowing him to use the beam. In our server design we decided to only allow players to carry one powerup at a time. Therefore, if a second 
	if gotten by the same player before the first was used. The first is now lost. Similarly, powerups do not persist after death. So if a player gets a powerup and it is not used, when she 
	dies she no longer has the powerup to use.

Server.cs:
	In server.cs we have our Server class which is started with the StartServerLoop() method (on a new thread), and that stays open for the lifetime of the thread. This methods starts
	the TcpListeners for both the Web clients and the Game clients. Let's step through real quick what the connection process looks like for each the web and Game clients:

	Game Client Connection:
		The first callback for the connection is OnPlayerConnect, this method simply prints out the statement: "Accepted new Client", changes the OnNetworkAction to OnReceiveName and
		calls Networking.GetData(). In OnReceiveName the received data is parsed to see if a valid name was given. All names are valid as long as they are non-empty strings, and are
		less than 16 characters in length. In our design, the names are (slightly) sanitized by removing all ';', '(', and ')'. If the name is empty then Networking.GetData() is called 
		again, WITHOUT, changing the callback, allowing the user to try sending a valid name. Likewise, if the name is longer than 16 characters our server simply trunkates it to 16 characters.
		If the player name received is valid then the player's ID, the WorldSize, and Walls are sent. If that send was successful, indicating that the user has successfully joined the game, 
		the SocketState is saved to our list of Valid SocketStates, a new Tank is added in the world, the OnNetworkAction is changed to ReceiveGameCommand and the event loop is continued
		by calling Networking.GetData(). 

	Web Client Connection: 
		The first callback of the handshake is OnWebConnect, which simply changes the OnNetworkAction to OnWebReceive and then Networking.GetData is called. In the OnWebReceive callback
		the HTTP request is pulled from the buffer, and passed to WebController.ParseHTTPRequest which returns the appropriate html page for the request, this is then sent to the client 
		with Networking.SendAndClose which closes the socket right after the send.

	Processing Messages From Clients:
		When a message from a GameClient is received the ReceiveGameCommand callback is called. The mechanics of this are simple, we keep a Dictionary of commands as they are received, 
		and if multiple commands from a tank are received in the span of one frame, the most recently sent is kept. The only scenario that a command is not overriden is if the 
		pre-existing command is the special DISC_CMD that indicates to the Model that the tank has disconnected. However, it should be noted that if a tank has disconnected
		no further commands should be received. 

	Broadcasting Frame To Clients:
		As mentioned several times in class we are using the StopWatch object and a busy loop to keep track of time in-between frame sends to the clients. After the specified number of miliseconds
		the method ServerGameWorld.UpdateGameWorld is called with the appropriate Dicionary of Commands, then BroadcastMessageAndRemoveDisconnected is called. The method, does exactly what it's name says. 
		The message of the world is broadcasted to every client, and if the send is unsuccessful then the client is considered to be disconnected, and the player will be removed from the game in the appropriate matter. 

ServerGameWorld:
	We have improved on our design from PS8. All properties, methods, and fields that were being shared by both the server and view were put into a parent GameModel class,
	and both the GameWorld (view class) and the ServerGameWorld (server class) inherit from it. 
	In the ServerGameWorld the method UpdateGameWorld is called once per frame, and it takes the dictionary of commands as a parameter, the method then processes all the commands
	in an order that we deemed most efficient, and logical in order for everything to happen as it should. 

	GameStat class:
		In order to keep all the relevant information for a player we decided to create a small class inside the ServerGame world called GameStat. This class stores the score,
		the shots taken, and the hit shots for each player. Unlike the Tank object, the GameStat is NEVER deleted for a player until the end of the game when they are stored in 
		the database. 
	

XML Settings:
	The server supports non-default values of tank width, wall width, tank speed, projectile speed, max powerup count, max powerup delay, and starting hp.  
	These can be changed by adding them in the XML settings file.  The server will expect custom values for these to correspond to the following XML tags: <tankwidth>, <wallwidth>, 
	<tankspeed>, <projectilespeed>, <maxpowerupcount>, <maxpowerupdelay>, and <startinghp>.  The tags are not case sensitive, i.e. <TankSpeed> or <tankspeed> will both work; however, 
	the respective opening and closing tags must have the same capitalizations or there will be an XML error.