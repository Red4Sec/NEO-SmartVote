"""
SmartVote - smart contract example
===================================

Author: belane
Email: belane@cityofzion.io

Date: 2018

"""

from boa.interop.Neo.Storage import Get, Put
from boa.interop.Neo.Runtime import Serialize, Deserialize
from boa.interop.System.ExecutionEngine import GetScriptContainer
from boa.interop.Neo.Transaction import GetReferences
from boa.interop.Neo.Output import GetScriptHash
from boa.builtins import concat


# get storage context
ctx = GetContext()


def Main(operation, args):
    """
    :param operation: str The name of the operation to perform (07)
    :param args: list of arguments along with the operation (10)
    :return: bool, result of the execution (01)
    doc: docs.neo.org/en-us/sc/tutorial/Parameter.html
    """

    if operation == 'register_proposal':
        return register_proposal(args)

    elif operation == 'vote':
        return vote(args)

    elif operation == 'count':
        return count(args)

    else:
        print("Unknown operation")
        return False


def register_proposal(args):
    """
    Register a New prosal
    :param args: list of arguments [PROPOSA ID, PROPOSAL DESCRIPTION, AUTHORIZED ADDR 1, AUTHORIZED ADDR N, ...]
    :return: bool, result of the execution
    """
    if len(args) < 4:
        return False

    # check proposal existence
    proposal_id = args[0]
    if Get(ctx, proposal_id):
        print("Already a Proposal")
        return False

    # proposal arguments
    proposal = []
    proposal.append(args[1])    # proposal description
    proposal.append(0)          # yes counter
    proposal.append(0)          # no counter

    # iterate from the third parameter to extract authorized addresses
    proposal_address = []
    index = 2
    while index < len(args):

        address = args[index]
        # check address format
        if len(address) != 20:
            print('bad address format')
            return False

        proposal_address.append(address)
        index += 1

    proposal.append(proposal_address) # authorized addresses

    # serialize proposal and write to storage
    proposal_storage = Serialize(proposal)
    Put(ctx, proposal_id, proposal_storage)

    return True

def vote(args):
    """
    Vote for a proposal
    :param args: list of arguments [PROPOSAL_ID, VOTE]
    :return: bool, result of the execution
    """
    if len(args) != 2:
        return False

    # get proposal from storage
    proposal_id = args[0]
    proposal_storage = Get(ctx, proposal_id)

    # check proposal existence
    if not proposal_storage:
        print("Proposal not found")
        return False

    # get caller address
    references = GetScriptContainer().References
    if len(references) < 1:
        return False
    caller_addr = references[0].ScriptHash

    # check if address already voted
    if Get(ctx, concat(proposal_id, caller_addr)):
        print('Already voted')
        return False

    # Deserialize proposal array
    proposal = Deserialize(proposal_storage)

    # iterate authorized address
    index = 0
    while index < len(proposal[3]):
        if proposal[3][index] == caller_addr:
            authorized = True
            break
        index += 1

    if not authorized:
        print('Not allowed to vote')
        return False

    # increment vote counter
    if args[1] == 1:
        print('Yes!')
        proposal[1] = proposal[1] + 1
    else:
        print('No!')
        proposal[2] = proposal[2] + 1

    # serialize proposal and write to storage
    proposal_storage = Serialize(proposal)
    Put(ctx, proposal_id, proposal_storage)

    # mark address as voted
    Put(ctx, concat(proposal_id, caller_addr), True)

    return True

def count(args):
    """
    Count the votes of a proposal
    :param args: list of arguments [PROPOSAL_ID]
    :return: bool, result of the execution
    """
    if len(args) != 1:
        return False

    # get proposal from storage
    proposal_id = args[0]
    proposal_storage = Get(ctx, proposal_id)

    # check proposal existence
    if not proposal_storage:
        print("Proposal not found")
        return False

    # deserialize proposal array
    proposal = Deserialize(proposal_storage)

    # get proposal description
    description = proposal[0]
    print(description)

    # get proposal votes
    votes_yes = proposal[1]
    votes_no = proposal[2]
    print(votes_yes)
    print(votes_no)

    return True
