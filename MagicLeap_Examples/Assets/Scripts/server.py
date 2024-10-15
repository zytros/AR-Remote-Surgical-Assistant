import socket
import struct

def start_server(ip_address, port):
    # Create a TCP/IP socket
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    # Bind the socket to the server's IP address and port
    server_address = (ip_address, port)
    print(f"Starting server on {ip_address}:{port}")
    server_socket.bind(server_address)

    # Listen for incoming connections
    server_socket.listen(1)  # Can queue up to 1 connection (adjust as needed)

    while True:
        print("Waiting for a connection...")
        connection, client_address = server_socket.accept()
        try:
            print(f"Connection established with {client_address}")

            # First, read the 4-byte length header
            length_header = connection.recv(4)

            if not length_header:
                print("No length header received. Closing connection.")
                continue

            # Unpack the length as a big-endian integer
            message_length = struct.unpack('>I', length_header)[0]
            print(f"Expecting message of length: {message_length} bytes")

            # Buffer to store the full message
            full_data = b""
            bytes_received = 0

            # Keep receiving until we've read the full message
            while bytes_received < message_length:
                data = connection.recv(4096)  # 4 KB buffer for each read
                if not data:
                    break  # No more data, exit the loop
                full_data += data
                bytes_received += len(data)

            if bytes_received == message_length:
                message = full_data.decode('utf-8')
                print(f"Received message: \n{message}")
                print(f'Length of message: {len(message)} bytes')

                # Optionally, send a response to the client
                response = "Message received!"
                connection.sendall(response.encode('utf-8'))
            else:
                print(f"Error: Only received {bytes_received}/{message_length} bytes")

        finally:
            # Close the connection with the client
            connection.close()
            print("Connection closed.")

def start_server_work(ip_address, port):
    # Create a TCP/IP socket
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    # Bind the socket to the server's IP address and port
    server_address = (ip_address, port)
    print(f"Starting server on {ip_address}:{port}")
    server_socket.bind(server_address)

    # Listen for incoming connections
    server_socket.listen(1)  # Can queue up to 1 connection (adjust as needed)

    while True:
        print("Waiting for a connection...")
        # Wait for a connection from the client
        connection, client_address = server_socket.accept()
        try:
            print(f"Connection established with {client_address}")

            # Receive the message from the client
            data1 = connection.recv(100000)  # Buffer size of 1024 bytes
            #print(f"Received {len(data)} bytes")
            if data1:
                message = data1.decode('utf-8')  # Decode bytes to string
                print(f"Received message: \n{message}")
                print(f'length of message: {len(message)}')
                # Optionally, send a response to the client
                response = "Message received!"
                connection.sendall(response.encode('utf-8'))
            else:
                print("No data received.")

        finally:
            # Close the connection with the client
            connection.close()
            print("Connection closed.")

# Example usage
if __name__ == "__main__":
    ip_address = '0.0.0.0'  # Listen on all available network interfaces
    port = 8030             # Port to listen on (should match the client)
    start_server(ip_address, port)
