# Open wallet
open wallet neo-privnet.wallet

# Wallet info
wallet

# Events=ON
config sc-events on

# Restore neo-python
docker exec -it neo-python /bin/bash
np-prompt -p

# Import contract
import contract /smart-contracts/SmartVote.avm 0710 01 true false

# Send Gas
send gas AUCoj2HZn8a8LD6LPE9Yv4kbNDkVmqyKEq 1000
