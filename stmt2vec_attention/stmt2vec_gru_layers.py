import theano
import theano.tensor as T
import numpy as np
from utils import _p, ortho_weight, norm_weight, tanh, linear


# GRU layer
def param_init_gru(  prefix='gru', nin=None, dim=None):
    #Gated Recurrent Unit (GRU)
    params = {}
    W = [norm_weight(nin,dim),norm_weight(nin,dim)]
    params[_p(prefix,'W')] = W
    params[_p(prefix,'b1')] = np.zeros((dim,)).astype('float64')
    params[_p(prefix,'b2')] = np.zeros((dim,)).astype('float64')
    U = [ortho_weight(dim),ortho_weight(dim)]
    params[_p(prefix,'U')] = U
    Wx = norm_weight(nin, dim)
    params[_p(prefix,'Wx')] = Wx
    Ux = ortho_weight(dim)
    params[_p(prefix,'Ux')] = Ux
    params[_p(prefix,'bx')] = np.zeros((dim,)).astype('float64')

    return params[_p(prefix,'W')][0], params[_p(prefix,'W')][1],  params[_p(prefix,'U')][0] , \
                        params[_p(prefix,'U')][1] , params[_p(prefix,'b1')], params[_p(prefix,'b2')], \
                        params[_p(prefix,'Wx')], params[_p(prefix,'Ux')], params[_p(prefix,'bx')]

def gru_layer(gruW1,gruW2, gruU1, gruU2, grub1, grub2, gruWx, gruUx, grubx, state_below, init_state, prefix='gru', mask=None, mark_reverse = False, **kwargs):
    #Feedforward pass through GRU
    nsteps = state_below.shape[0]

    dim =  grub1.shape[0]
    if mark_reverse: state_below = state_below[::-1,:]
    if init_state == None:
        init_state = T.alloc(np.float64(0.), dim)

    if mask == None:
        mask = T.alloc(np.float64(1.0),  state_below.shape[0])

    state_below_1 = T.dot(state_below, gruW1) + grub1
    state_below_2 = T.dot(state_below, gruW2) + grub2
    state_belowx = T.dot(state_below, gruWx) + grubx
    U1 = gruU1
    U2 = gruU2
    Ux = gruUx

    def _step_slice(m_, x_1,x_2, xx_, h_, U1,U2, Ux):
        preact1 = T.dot(h_, U1)
        preact2 = T.dot(h_, U2)
        preact1 += x_1
        preact2 += x_2

        r = T.nnet.sigmoid(preact1)
        u = T.nnet.sigmoid( preact2)

        preactx = T.dot(h_, Ux)
        preactx = preactx * r
        preactx = preactx + xx_

        h = T.tanh(preactx)

        h = u * h_ + (np.float64(1.0) - u) * h
        #h = m_[:,None] * h + (np.float(1.0)- m_)[:,None] * h_

        return h

    seqs = [mask, state_below_1,state_below_2, state_belowx]
    _step = _step_slice

    rval, updates = theano.scan(_step,
                                sequences=seqs,
                                outputs_info = [init_state],
                                non_sequences = [gruU1,gruU2,
                                                 gruUx],
                                name='encoder_layers',
                                n_steps=nsteps,
                                profile=False,
                                strict=True)


    return rval, nsteps


