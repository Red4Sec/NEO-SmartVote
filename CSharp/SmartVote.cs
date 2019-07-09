using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;

namespace Neo.SmartContract
{
    public class SmartVote : Framework.SmartContract
    {
        /// <summary>
        /// SmartContract's Entry Point
        /// Parameter list:
        ///     https://docs.neo.org/en-us/sc/Parameter.html
        /// </summary>
        /// <param name="operation">The name of the operation to perform (07)</param>
        /// <param name="args">List of arguments along with the operation(10)</param>
        /// <returns>result of the execution(10)</returns>
        public static object Main(string operation, params object[] args)
        {
            if (operation == "register_proposal") return Register_proposal(args);
            else if (operation == "vote") return Vote(args);
            else if (operation == "count") return Count(args);

            return false;
        }

        /// <summary>
        /// Register a New prosal
        /// </summary>
        /// <param name="args">list of arguments [PROPOSA ID, PROPOSAL TEXT, AUTHORIZED ADDR 1, AUTHORIZED ADDR N, ...]</param>
        /// <returns>Result of the execution</returns>
        static bool Register_proposal(object[] args)
        {
            // check proposal existence
            string proposal_id = (string)args[0];
            StorageContext ctx = Storage.CurrentContext;
            if (Storage.Get(ctx, proposal_id).Length != 0)
            {
                Runtime.Log("Already a Proposal");
                return false;
            }

            // proposal arguments
            object[] proposal = new object[4];
            proposal[0] = args[1];    // description text
            proposal[1] = "0";        // yes counter
            proposal[2] = "0";        // no counter

            // iterate from the third parameter to extract authorized address
            int index = 2;
            string[] proposal_address = new string[args.Length - 2];
            while (index < args.Length)
            {
                string address = (string)args[index];
                // check address format
                if (address.Length != 20)
                {
                    Runtime.Log("bad address format");
                    return false;
                }

                proposal_address[index - 2] = address;
                index += 1;
            }
            proposal[3] = proposal_address; // authorized address

            // serialize proposal and write to storage
            byte[] proposal_storage = proposal.Serialize();
            Storage.Put(ctx, proposal_id, proposal_storage);

            return true;
        }

        /// <summary>
        /// Vote for a proposal
        /// </summary>
        /// <param name="args">list of arguments [PROPOSAL_ID, VOTE]</param>
        /// <returns>result of the execution</returns>
        static bool Vote(object[] args)
        {
            if (args.Length != 2)
                return false;

            // get proposal from storage
            byte[] proposal_id = (byte[])args[0];
            int vote = (int)args[1];

            // Extract caller
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] inputs = tx.GetReferences();
            TransactionOutput reference = inputs[0];
            byte[] caller = reference.ScriptHash;

            StorageContext ctx = Storage.CurrentContext;
            byte[] proposal_storage = Storage.Get(ctx, proposal_id);

            // check proposal existence
            if (proposal_storage.Length == 0)
            {
                Runtime.Log("Proposal not found");
                return false;
            }

            // check if address already voted
            byte[] key = proposal_id.Concat(caller);
            if (Storage.Get(ctx, key).Length > 0)
            {
                Runtime.Log("Already voted");
                return false;
            }

            // Deserialize proposal array
            object[] proposal = (object[])proposal_storage.Deserialize();

            // check if tx is right signed
            if (!Runtime.CheckWitness(caller))
            {
                Runtime.Log("You are not who you say");
                return false;
            }

            // iterate authorized address
            int index = 0;
            bool authorized = false;
            while (index < ((string[])proposal[3]).Length)
            {
                // for address in proposal[3]:
                if ((object)((string[])proposal[3])[index] == caller)
                {
                    authorized = true;
                    break;
                }
                index += 1;
            }
            if (!authorized)
            {
                Runtime.Log("Not allowed to vote");
                return false;
            }

            // increment vote counter
            if (vote == 1)
            {
                Runtime.Log("Yes!");
                proposal[1] = (int)proposal[1] + 1;
            }
            else
            {
                Runtime.Log("No!");
                proposal[2] = (int)proposal[2] + 1;
            }

            // serialize proposal and write to storage
            proposal_storage = proposal.Serialize();
            Storage.Put(ctx, proposal_id, proposal_storage);

            // mark address as voted
            Storage.Put(ctx, key, 1);
            return true;
        }

        /// <summary>
        /// Count the votes of a proposal
        /// </summary>
        /// <param name="args">list of arguments [PROPOSAL_ID]</param>
        /// <returns>Result of the execution</returns>
        static object Count(object[] args)
        {
            if (args.Length != 1)
                return false;

            // get proposal from storage
            byte[] proposal_id = (byte[])args[0];
            StorageContext ctx = Storage.CurrentContext;
            byte[] proposal_storage = Storage.Get(ctx, proposal_id);

            // check proposal existence
            if (proposal_storage == null || proposal_storage.Length == 0)
            {
                Runtime.Log("Proposal not found");
                return false;
            }

            // Deserialize proposal array
            object[] proposal = (object[])proposal_storage.Deserialize();

            // get proposal description
            Runtime.Log((string)proposal[0]);

            // get proposal votes
            Runtime.Log((string)proposal[1]);
            Runtime.Log((string)proposal[2]);

            return proposal;
        }
    }
}