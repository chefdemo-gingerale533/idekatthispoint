import requests
import json

# Base URL of the Flask API
BASE_URL = "http://example.com:5000/api/licenses"  # Replace "example.com" with your domain or "127.0.0.1" for local testing

def send_post_request():
    """
    Sends a POST request to the Flask API to add license data
    """
    # Data to be sent in the POST request
    data = {
        "RobloxUsername": "exampleRobloxUser",
        "DiscordUsername": "example#1234",
        "Items": [
            {"Name": "Coke", "Price": 1.5},
            {"Name": "Burger", "Price": 5.0}
        ],
        "Total": 6.5,
        "PurchaseDate": "2025-05-11T00:00:00"
    }

    try:
        # Send POST request
        response = requests.post(BASE_URL, headers={"Content-Type": "application/json"}, data=json.dumps(data))

        # Check response status
        if response.status_code == 201:
            print("POST Request Successful:")
            print(response.json())
        else:
            print(f"POST Request Failed. Status Code: {response.status_code}")
            print(response.text)

    except requests.exceptions.RequestException as e:
        print(f"An error occurred: {e}")

def send_get_request():
    """
    Sends a GET request to the Flask API to retrieve all licenses
    """
    try:
        # Send GET request
        response = requests.get(BASE_URL)

        # Check response status
        if response.status_code == 200:
            print("GET Request Successful:")
            licenses = response.json()
            for license in licenses:
                print(json.dumps(license, indent=4))
        else:
            print(f"GET Request Failed. Status Code: {response.status_code}")
            print(response.text)

    except requests.exceptions.RequestException as e:
        print(f"An error occurred: {e}")

if __name__ == "__main__":
    print("Choose an action:")
    print("1. Send POST Request")
    print("2. Send GET Request")

    choice = input("Enter your choice (1 or 2): ").strip()

    if choice == "1":
        send_post_request()
    elif choice == "2":
        send_get_request()
    else:
        print("Invalid choice. Exiting.")
