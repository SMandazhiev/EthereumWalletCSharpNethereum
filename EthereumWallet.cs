using System;
using static System.Console;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nethereum.HdWallet;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NBitcoin;
using Rijndael256;

namespace EthereumWallet
{
    public class EthereumWalletNetCore
    {
        const string network = "https://ropsten.infura.io"; // In this example we will use ropsten testnet
        const string workingDirectory = @"Wallets\";
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }
        static async Task MainAsync(string[] args)
        {
            Web3 web3 = new Web3(network);
            Directory.CreateDirectory(workingDirectory);// Path to the directory containing Wallets
            string[] valiableOperations = {
                "create", "load", "recover", "exit" // Allowed functionality
            };

            string input = string.Empty;
            bool isWalletReady = false;
            Wallet wallet = new Wallet(Wordlist.English, WordCount.Twelve);
            while (!input.ToLower().Equals("exit"))
            {
                /* First the user must load working wallet. This can happen in three ways:
                   Create brand-new wallet.
                   Load existing wallet from json file by enter his name and password.
                   Recover existing wallet from mnemonic phrase (words).*/
                if (!isWalletReady)
                {
                    do
                    {
                        input = ReceiveCommandCreateLoadOrRecover();

                    } while (!((IList)valiableOperations).Contains(input));
                    switch (input)
                    {
                        // Create brand-new wallet. User will receive mnemonic phrase, public and private keys.
                        case "create":
                            wallet = CreateWalletDialog();
                            isWalletReady = true;
                            break;

                        // Load wallet from json file contains encrypted mnemonic phrase (words).
                        // This command will decrypt words and load wallet.
                        case "load":
                            wallet = LoadWalletDialog();
                            isWalletReady = true;
                            break;

                        /* Recover wallet from mnemonic phrase (words) which user must enter.
                         This is usefull if user has wallet, but has no json file for him 
                         (for example if he uses this program for the first time).
                         Command will creates new Json file contains encrypted mnemonic phrase (words)
                         for this wallet.
                         After encrypt words program will load wallet.*/
                        case "recover":
                            wallet = RecoverWalletDialog();
                            isWalletReady = true;
                            break;

                        // Exit from the program.
                        case "exit":
                            return;
                    }
                }
                else // When wallet is already loaded user can operates with it.
                {
                    string[] valiableCommands = {
                    "balance", "receive", "send", "exit" //Allowed functionality
                    };
                    string inputCommand = string.Empty;
                    while (!inputCommand.ToLower().Equals("exit"))
                    {
                        do
                        {
                            inputCommand = ReceiveCommandForEthersOperations();

                        } while (!((IList)valiableCommands).Contains(inputCommand));
                        switch (inputCommand)
                        {
                            // Send transaction from address to address
                            case "send":
                                await SendTransactionDialog(wallet);
                                break;

                            // Shows the balances of addresses and total balance.
                            case "balance":
                                await GetWalletBallanceDialog(web3, wallet);
                                break;

                            // Shows the addresses in which you can receive coins.
                            case "receive":
                                Receive(wallet);
                                break;
                            case "exit":
                                return;
                        }
                    }
                }
            }
        }

        private static string ReceiveCommandCreateLoadOrRecover()
        {
            WriteLine("Choose working wallet.");
            WriteLine("Choose [create] to Create new Wallet.");
            WriteLine("Choose [load] to load existing Wallet from file.");
            WriteLine("Choose [recover] to recover Wallet with Mnemonic Phrase.");
            Write("Enter operation [\"Create\", \"Load\", \"Recover\", \"Exit\"]: ");
            string input = ReadLine().ToLower().Trim();
            return input;
        }

        private static Wallet CreateWalletDialog()
        {
            try
            {
                string password;
                string passwordConfirmed;
                do
                {
                    Write("Enter password for encryption: ");
                    password = ReadLine();
                    Write("Confirm password: ");
                    passwordConfirmed = ReadLine();
                    if (password != passwordConfirmed)
                    {
                        WriteLine("Passwords did not match!");
                        WriteLine("Try again.");
                    }
                } while (password != passwordConfirmed);

                Wallet wallet = CreateWallet(password, workingDirectory);
                return (wallet);
            }
            catch (Exception)
            {
                WriteLine($"ERROR! Wallet in path {workingDirectory} can`t be created!");
                throw;
            }
        }

        private static Wallet LoadWalletDialog()
        {
            Write("Enter: Name of the file containing wallet: ");
            string nameOfWallet = ReadLine();
            Write("Enter: Password: ");
            string pass = ReadLine();
            try
            {
                Wallet wallet = LoadWalletFromJsonFile(nameOfWallet, workingDirectory, pass);
                return (wallet);

            }
            catch (Exception e)
            {
                WriteLine($"ERROR! Wallet {nameOfWallet} in path {workingDirectory} can`t be loaded!");
                throw e;
            }
        }

        private static Wallet RecoverWalletDialog()
        {
            try
            {
                Write("Enter: Mnemonic words with single space separator: ");
                string mnemonicPhrase = ReadLine();
                Write("Enter: password for encryption: ");
                string passForEncryptionInJsonFile = ReadLine();
                Wallet wallet = RecoverFromMnemonicPhraseAndSaveToJson(
                    mnemonicPhrase, passForEncryptionInJsonFile, workingDirectory);
                return wallet;
            }
            catch (Exception e)
            {
                WriteLine("ERROR! Wallet can`t be recovered! Check your mnemonic phrase.");
                throw e;
            }
        }

        private static string ReceiveCommandForEthersOperations()
        {
            Write("Enter operation [\"Balance\", \"Receive\", \"Send\", \"Exit\"]: ");
            string inputCommand = ReadLine().ToLower().Trim();
            return inputCommand;
        }

        private static async Task SendTransactionDialog(Wallet wallet)
        {
            WriteLine("Enter: Address sending ethers.");
            string fromAddress = ReadLine();
            WriteLine("Enter: Address receiving ethers.");
            string toAddress = ReadLine();
            WriteLine("Enter: Amount of coins in ETH.");
            double amountOfCoins = 0d;
            try
            {
                amountOfCoins = double.Parse(ReadLine());
            }
            catch (Exception)
            {
                WriteLine("Unacceptable input for amount of coins.");
            }
            if (amountOfCoins > 0.0d)
            {
                WriteLine($"You will send {amountOfCoins} ETH from {fromAddress} to {toAddress}");
                WriteLine($"Are you sure? yes/no");
                string answer = ReadLine();
                if (answer.ToLower() == "yes")
                {
                    await Send(wallet, fromAddress, toAddress, amountOfCoins);
                }
            }
            else
            {
                WriteLine("Amount of coins for transaction must be positive number!");
            }
        }

        private static async Task GetWalletBallanceDialog(Web3 web3, Wallet wallet)
        {
            WriteLine("Balance:");
            try
            {
                await Balance(web3, wallet);
            }
            catch (Exception)
            {
                WriteLine("Error occured! Check your wallet.");
            }
        }

        /// <summary>
        /// Create brand-new Wallet and give you mnemonic phrase public and private keys
        /// </summary>
        /// <param name="password">The password for encryption of mnemonic phrase in the json file
        /// containing encrypted mnemonic phrace and data of creation.</param>
        /// <param name="pathfile">The path to directory where the file will be saved. For example @"Wallets/".</param>
        /// <returns>New wallet.</returns>
        public static Wallet CreateWallet(string password, string pathfile)
        {
            Wallet wallet = new Wallet(Wordlist.English, WordCount.Twelve); // Create brand-new wallet
            string words = string.Join(" ", wallet.Words);
            string fileName = string.Empty;
            try
            {
                fileName = SaveWalletToJsonFile(wallet, password, pathfile);
            }
            catch (Exception e)
            {
                WriteLine($"ERROR! The file {fileName} can`t be saved! {e}");
                throw e;
            }

            WriteLine("New Wallet was created successfully!");
            WriteLine("Write down the following mnemonic words and keep them in the save place.");
            WriteLine(words);
            WriteLine("Seed: ");
            WriteLine(wallet.Seed);
            WriteLine();
            PrintAddressesAndKeys(wallet);
            return wallet;
        }

        /// <summary>
        /// Print addresses and private keys of the given wallet.
        /// </summary>
        /// <param name="wallet">Working wallet.</param>
        private static void PrintAddressesAndKeys(Wallet wallet)
        {
            WriteLine("Addresses:");
            for (int i = 0; i < 20; i++)
            {
                WriteLine(wallet.GetAccount(i).Address);
            }

            WriteLine();
            WriteLine("Private Keys:");
            for (int i = 0; i < 20; i++)
            {
                WriteLine(wallet.GetAccount(i).PrivateKey);
            }

            WriteLine();
        }

        /// <summary>
        /// Save Wallet to Json file. File contains encrupted mnemonic phrase and date of creation. 
        /// </summary>
        /// <param name="wallet">The wallet which mnemonic phrase will be encrupted and saved in json file.</param>
        /// <param name="password">The password for encryption of mnemonic phrase in the json file
        /// containing encrypted mnemonic phrace and data of creation.</param>
        /// <param name="pathfile">The path to directory where the file will be saved. For example @"Wallets/".</param>
        /// <returns>Name of the json file.</returns>
        public static string SaveWalletToJsonFile(Wallet wallet, string password, string pathfile)
        {
            string words = string.Join(" ", wallet.Words);
            var encryptedWords = Rijndael.Encrypt(words, password, KeySize.Aes256); // Encrypt the Mnemonic phrase
            string date = DateTime.Now.ToString();
            // Anonymous object containing encryptedWords and date will be writen in the Json file
            var walletJsonData = new { encryptedWords = encryptedWords, date = date };
            string json = JsonConvert.SerializeObject(walletJsonData);
            Random random = new Random();
            var fileName =
                "EthereumWallet_"
                + DateTime.Now.Year + "-"
                + DateTime.Now.Month + "-"
                + DateTime.Now.Day + "_"
                + DateTime.Now.Hour + "_"
                + DateTime.Now.Minute + "_"
                + DateTime.Now.Second + "_"
                + random.Next(0, 10000) + ".json";
            File.WriteAllText(Path.Combine(pathfile, fileName), json);
            WriteLine($"Wallet saved in file: {fileName}");
            return fileName;
        }

        /// <summary>
        /// Read saved encrypted wallet mnemonic phrase from json file. 
        /// Decrypt it and load wallet.
        /// </summary>
        /// <param name="nameOfWalletFile">The name of json file containing wallet info.</param>
        /// <param name="path">Path to the file. For example: @"Wallets\". </param>
        /// <param name="pass">Password to decrypt the wallet mnemonic phrase.</param>
        /// <returns>Wallet loaded from json file.</returns>
        static Wallet LoadWalletFromJsonFile(string nameOfWalletFile, string path, string pass)
        {
            string pathToFile = Path.Combine(path, nameOfWalletFile);
            string words = string.Empty;
            // Read from fileName
            WriteLine($"Read from {pathToFile}");
            try
            {
                string line = File.ReadAllText(pathToFile);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(line);
                string encryptedWords = results.encryptedWords;
                words = Rijndael.Decrypt(encryptedWords, pass, KeySize.Aes256);
                string dataAndTime = results.date;
            }
            catch (Exception e)
            {
                WriteLine("ERROR!" + e);
            }

            return Recover(words);
        }

        /// <summary>
        /// Recover wallet from mnemonic phrace.
        /// </summary>
        /// <param name="words">Mnemonic phrase, for example "forest map embrace kitchen probe"</param>
        /// <returns>Name of the json file.</returns>
        public static Wallet Recover(string words)
        {
            Wallet wallet = new Wallet(words, null);
            WriteLine("Wallet was successfully recovered.");
            WriteLine("Words: " + string.Join(" ", wallet.Words));
            WriteLine("Seed: " + string.Join(" ", wallet.Seed));
            WriteLine();
            PrintAddressesAndKeys(wallet);
            return wallet;
        }

        /// <summary>
        /// Recover wallet from mnemonic phrace and save it to Json file in the given directory.
        /// </summary>
        /// <param name="words">Mnemonic phrase, for example "forest map embrace kitchen probe"</param>
        /// <param name="password">The password for encryption of mnemonic phrase in the json file
        /// containing encrypted mnemonic phrace and data of creation.</param>
        /// <param name="pathfile">The path to directory where the file will be saved. For example @"Wallets/".</param>
        /// <returns></returns>
        public static Wallet RecoverFromMnemonicPhraseAndSaveToJson(string words, string password, string pathfile)
        {
            Wallet wallet = Recover(words);
            string fileName = string.Empty;
            try
            {
                fileName = SaveWalletToJsonFile(wallet, password, pathfile);
            }
            catch (Exception)
            {
                WriteLine($"ERROR! The file {fileName} with recovered wallet can`t be saved!");
                throw;
            }

            return wallet;
        }

        /// <summary>
        /// Show addresses in which you can receive coins.
        /// </summary>
        /// <param name="wallet">Valid ethereum wallet.</param>
        public static void Receive(Wallet wallet)
        {
            if (wallet.GetAddresses().Count() > 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    WriteLine(wallet.GetAccount(i).Address);
                }

                WriteLine();
            }
            else
            {
                WriteLine("No addresses founded!");
            }
        }

        /// <summary>
        /// Sending transaction.
        /// </summary>
        /// <param name="fromAddress">Address sending coins.</param>
        /// <param name="toAddress">Address receiving coins.</param>
        /// <returns></returns>
        private static async Task Send(Wallet wallet, string fromAddress, string toAddress, double amountOfCoins)
        {
            Account accountFrom = wallet.GetAccount(fromAddress);
            string privateKeyFrom = accountFrom.PrivateKey;
            if (privateKeyFrom == string.Empty)
            {
                WriteLine("Address sending coins is not from current wallet!");
                throw new Exception("Address sending coins is not from current wallet!");
            }

            var web3 = new Web3(accountFrom, network);
            var wei = Web3.Convert.ToWei(amountOfCoins);
            try
            {
                var transaction =
                                await web3.TransactionManager
                                .SendTransactionAsync(
                                    accountFrom.Address,
                                    toAddress,
                                    new Nethereum.Hex.HexTypes.HexBigInteger(wei));
                WriteLine("Transaction has been sent successfully!");
            }
            catch (Exception e)
            {
                WriteLine($"ERROR! The transaction can`t be completed! {e}");
                throw e;
            }
        }

        /// <summary>
        /// Show total balance and balance of eache address.
        /// </summary>
        /// <param name="web3">Nethereum web3 object.</param>
        /// <param name="wallet">Valid ethereum wallet.</param>
        /// <returns></returns>
        private static async Task Balance(Web3 web3, Wallet wallet)
        {
            decimal totalBalance = 0.0m;
            for (int i = 0; i < 20; i++)
            {
                var balance = await web3.Eth.GetBalance.SendRequestAsync(wallet.GetAccount(i).Address);
                var etherAmount = Web3.Convert.FromWei(balance.Value);
                totalBalance += etherAmount;
                WriteLine(wallet.GetAccount(i).Address + " " + etherAmount + " ETH");
            }

            WriteLine($"Total balance: {totalBalance} ETH \n");
        }
    }
}
