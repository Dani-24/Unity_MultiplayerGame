<?php

$servername = "localhost";
$username = "danieltr1";
$password = "aLZsQgYa6q9q";
$dbname = "danieltr1";

if ($_SERVER["REQUEST_METHOD"] == "POST") {

    if(ConnectToServer() == false)  return;

    $methodToCall = $_POST["methodToCall"];

    switch((string)$methodToCall)
    {
        case "Host Room":
            HostRoom();
            break;
        case "Search Room":
            SearchRoom();
            break;
        case "Send Host Data":
            SendHostData();
            break;
        case "Receive Host Data":
            ReceiveHostData();
            break;
        case "Send Client Data":
            SendClientData();
            break;
        case "Receive Client Data":
            ReceiveClientData();
            break;
    }

    CloseConnection();
} 
else 
{
   echo "PHP: Método no permitido \n";
}

function HostRoom(){
    $timeStamp  = $_POST["timeStamp"];
    $host       = $_POST["host"];
    $close      = $_POST["close"];
    $id         = $_POST["roomId"];

    global $conn;

    if(!$close) $sql    = "INSERT INTO Rooms (Host, Date) VALUES ('$host', '$timeStamp')";
    else $sql           = "DELETE FROM Rooms WHERE Id = $id";

    if ($conn->query($sql) === TRUE)    echo $conn->insert_id;
    else                                echo "PHP: Room creation/deletion Error " . mysqli_error($conn);
}

function SearchRoom(){
    global $conn;

    $sql = "SELECT * FROM Rooms";

    $result = $conn->query($sql);

    if ($result->num_rows > 0) 
    {
        while ($row = $result->fetch_assoc()) 
            echo json_encode($row) . "\n";
    }
    else 
        echo "No Rooms Available";
}

function SendHostData(){

}

function ReceiveHostData(){

}

function SendClientData(){

}   

function ReceiveClientData(){

}

function ConnectToServer() 
{
   global $servername, $username, $password, $dbname;
   global $conn;
   
   $conn = new mysqli($servername, $username, $password, $dbname);

   if ($conn->connect_error) {
       die("PHP: Conexión fallida: " . $conn->connect_error);
   }

   return true;
}

function CloseConnection() 
{
   global $conn;
   $conn->close();
}

?>