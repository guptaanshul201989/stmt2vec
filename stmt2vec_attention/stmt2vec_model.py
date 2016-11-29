import theano
from theano import tensor as T
import numpy as np
from theano.tensor.shared_randomstreams import RandomStreams
from optimization import nesterov_rmsprop_multiple, logsumexp, dropout_multiple, log_softmax
from stmt2vec_gru_layers import  param_init_gru, gru_layer
from theano import printing

class CopyStmtRecurrentAttentionalModel(object):
    NONE = "%NONE%"
    def __init__(self, hyperparameters, all_voc_size, empirical_name_dist, naming_data):
        self.D = hyperparameters["D"]
        self.hyperparameters = hyperparameters
        self.__check_all_hyperparmeters_exist()
        self.all_voc_size = all_voc_size
        self.naming_data =  naming_data
        self.__init_parameter(empirical_name_dist)

    def __init_parameter(self, empirical_name_dist):
        all_name_rep = np.random.randn(self.all_voc_size, self.D).astype('float32') * 10 ** self.hyperparameters["log_name_rep_init_scale"]
        self.all_name_reps = theano.shared(all_name_rep, name="code_name_reps")

        # By convention, the last one is NONE, which is never predicted.
        self.name_bias = theano.shared(np.log(empirical_name_dist)[:-1].astype(np.float32), name="name_bias")

        # GRU, stmt2vec layers
        fgruW1,fgruW2, fgruU1, fgruU2, fgrub1,fgrub2, fgruWx, fgruUx, fgrubx =  param_init_gru( prefix='encoder',nin=128, dim=self.hyperparameters["gru1_dim"])
        self.fgruW1= theano.shared(fgruW1, name="fgruW1")
        self.fgruW2= theano.shared(fgruW2, name="fgruW2")
        self.fgruU1 = theano.shared(fgruU1, name="fgruU1")
        self.fgruU2 = theano.shared(fgruU2, name="fgruU2")
        self.fgrub1 = theano.shared(fgrub1, name="fgrub1")
        self.fgrub2 = theano.shared(fgrub2, name="fgrub2")
        self.fgruWx= theano.shared(fgruWx, name="fgruWx")
        self.fgruUx = theano.shared(fgruUx, name="fgruUx")
        self.fgrubx = theano.shared(fgrubx, name="fgrubx")
        
        bgruW1,bgruW2, bgruU1, bgruU2, bgrub1,bgrub2, bgruWx, bgruUx, bgrubx =  param_init_gru( prefix='encoder',nin=128, dim=self.hyperparameters["gru1_dim"])
        self.bgruW1= theano.shared(bgruW1, name="bgruW1")
        self.bgruW2= theano.shared(bgruW2, name="bgruW2")
        self.bgruU1 = theano.shared(bgruU1, name="bgruU1")
        self.bgruU2 = theano.shared(bgruU2, name="bgruU2")
        self.bgrub1 = theano.shared(bgrub1, name="bgrub1")
        self.bgrub2 = theano.shared(bgrub2, name="bgrub2")
        self.bgruWx= theano.shared(bgruWx, name="bgruWx")
        self.bgruUx = theano.shared(bgruUx, name="bgruUx")
        self.bgrubx = theano.shared(bgrubx, name="bgrubx")
        
        gru_layer1_bias = np.random.randn(self.hyperparameters["gru1_dim"] *2).astype('float32') * 10 ** self.hyperparameters["log_layer1_init_scale"]
        self.gru_layer1_bias = theano.shared(gru_layer1_bias, name="gru_layer1_bias")
        
        fgrurecW1,fgrurecW2, fgrurecU1, fgrurecU2, fgrurecb1,fgrurecb2, fgrurecWx, fgrurecUx, fgrurecbx =  param_init_gru( prefix='encoder',nin=self.hyperparameters["gru1_dim"]*2 + 128, dim=self.hyperparameters["gru2_dim"])
        self.fgrurecW1= theano.shared(fgrurecW1, name="fgrurecW1")
        self.fgrurecW2= theano.shared(fgrurecW2, name="fgrurecW2")
        self.fgrurecU1 = theano.shared(fgrurecU1, name="fgrurecU1")
        self.fgrurecU2 = theano.shared(fgrurecU2, name="fgrurecU2")
        self.fgrurecb1 = theano.shared(fgrurecb1, name="fgrurecb1")
        self.fgrurecb2 = theano.shared(fgrurecb2, name="fgrurecb2")
        self.fgrurecWx= theano.shared(fgrurecWx, name="fgrurecWx")
        self.fgrurecUx = theano.shared(fgrurecUx, name="fgrurecUx")
        self.fgrurecbx = theano.shared(fgrurecbx, name="fgrurecbx")
        
        bgrurecW1,bgrurecW2, bgrurecU1, bgrurecU2, bgrurecb1,bgrurecb2, bgrurecWx, bgrurecUx, bgrurecbx =  param_init_gru( prefix='encoder',nin=self.hyperparameters["gru1_dim"]*2 +128, dim=self.hyperparameters["gru2_dim"])
        self.bgrurecW1= theano.shared(bgrurecW1, name="bgrurecW1")
        self.bgrurecW2= theano.shared(bgrurecW2, name="bgrurecW2")
        self.bgrurecU1 = theano.shared(bgrurecU1, name="bgrurecU1")
        self.bgrurecU2 = theano.shared(bgrurecU2, name="bgrurecU2")
        self.bgrurecb1 = theano.shared(bgrurecb1, name="bgrurecb1")
        self.bgrurecb2 = theano.shared(bgrurecb2, name="bgrurecb2")
        self.bgrurecWx= theano.shared(bgrurecWx, name="bgrurecWx")
        self.bgrurecUx = theano.shared(bgrurecUx, name="bgrurecUx")
        self.bgrurecbx = theano.shared(bgrurecbx, name="bgrurecbx")


        gru_layerrec_bias = np.random.randn(self.hyperparameters["gru2_dim"] *2).astype('float32') * 10 ** self.hyperparameters["log_layer2_init_scale"]
        self.gru_layerrec_bias = theano.shared(gru_layerrec_bias, name="gru_layerrec_bias")


        # Probability that each token will be copied
        conv_layer3_code = np.random.randn(1, self.hyperparameters["gru2_dim"] *2,
                                     self.hyperparameters["layer3_window_size"], 1).astype('float32') * 10 ** self.hyperparameters["log_layer3_init_scale"]
        self.conv_layer3_copy_code = theano.shared(conv_layer3_code, name="conv_layer3_copy_code")
        conv_layer3_bias = np.random.randn(1).astype('float32') * 10 ** self.hyperparameters["log_layer3_init_scale"]
        self.conv_layer3_copy_bias = theano.shared(conv_layer3_bias[0], name="conv_layer3_copy_bias")

        # Probability that we do a copy
        conv_copy_code = np.random.randn(1, self.hyperparameters["gru2_dim"] *2,
                                     self.hyperparameters["layer3_window_size"], 1).astype('float32') * 10 ** self.hyperparameters["log_copy_init_scale"]
        self.conv_copy_code = theano.shared(conv_copy_code, name="conv_copy_code")
        conv_copy_bias = np.random.randn(1).astype('float32') * 10 ** self.hyperparameters["log_copy_init_scale"]
        self.conv_copy_bias = theano.shared(conv_copy_bias[0], name="conv_copy_bias")

        # Attention vectors
        conv_layer3_att_code = np.random.randn(1, self.hyperparameters["gru2_dim"] *2,
                                     self.hyperparameters["layer3_window_size"], 1).astype('float32') * 10 ** self.hyperparameters["log_layer3_init_scale"]
        self.conv_layer3_att_code = theano.shared(conv_layer3_att_code, name="conv_layer3_att_code")
        conv_layer3_att_bias = np.random.randn(1).astype('float32') * 10 ** self.hyperparameters["log_layer3_init_scale"]
        self.conv_layer3_att_bias = theano.shared(conv_layer3_att_bias[0], name="conv_layer3_att_bias")

        # Recurrent layer
        gru_prev_hidden_to_next = np.random.randn(self.hyperparameters["gru2_dim"] *2, self.hyperparameters["gru2_dim"] *2).astype('float32')\
                                * 10 ** self.hyperparameters["log_hidden_init_scale"]
        self.gru_prev_hidden_to_next = theano.shared(gru_prev_hidden_to_next, name="gru_prev_hidden_to_next")
        gru_prev_hidden_to_reset = np.random.randn(self.hyperparameters["gru2_dim"] *2, self.hyperparameters["gru2_dim"] *2).astype('float32')\
                                * 10 ** self.hyperparameters["log_hidden_init_scale"]
        self.gru_prev_hidden_to_reset = theano.shared(gru_prev_hidden_to_reset, name="gru_prev_hidden_to_reset")
        gru_prev_hidden_to_update = np.random.randn(self.hyperparameters["gru2_dim"] *2, self.hyperparameters["gru2_dim"] *2).astype('float32')\
                                * 10 ** self.hyperparameters["log_hidden_init_scale"]
        self.gru_prev_hidden_to_update = theano.shared(gru_prev_hidden_to_update, name="gru_prev_hidden_to_update")

        gru_prediction_to_reset = np.random.randn(self.D, self.hyperparameters["gru2_dim"] *2).astype('float32') * 10 ** self.hyperparameters["log_hidden_init_scale"]
        self.gru_prediction_to_reset = theano.shared(gru_prediction_to_reset, name="gru_prediction_to_reset")

        gru_prediction_to_update = np.random.randn(self.D, self.hyperparameters["gru2_dim"] *2).astype('float32') * 10 ** self.hyperparameters["log_hidden_init_scale"]
        self.gru_prediction_to_update = theano.shared(gru_prediction_to_update, name="gru_prediction_to_update")

        gru_prediction_to_hidden = np.random.randn(self.D, self.hyperparameters["gru2_dim"] *2).astype('float32') * 10 ** self.hyperparameters["log_hidden_init_scale"]
        self.gru_prediction_to_hidden = theano.shared(gru_prediction_to_hidden, name="gru_prediction_to_hidden")

        gru_hidden_update_bias = np.random.randn(self.hyperparameters["gru2_dim"] *2).astype('float32') * 10 ** self.hyperparameters["log_layer2_init_scale"]
        self.gru_hidden_update_bias = theano.shared(gru_hidden_update_bias, name="gru_hidden_update_bias")
        gru_update_bias = np.random.randn(self.hyperparameters["gru2_dim"] *2).astype('float32') * 10 ** self.hyperparameters["log_layer2_init_scale"]
        self.gru_update_bias = theano.shared(gru_update_bias, name="gru_update_bias")
        gru_reset_bias = np.random.randn(self.hyperparameters["gru2_dim"] *2).astype('float32') * 10 ** self.hyperparameters["log_layer2_init_scale"]
        self.gru_reset_bias = theano.shared(gru_reset_bias, name="gru_reset_bias")

        h0 = np.random.randn(3* self.hyperparameters["gru1_dim"] *2, self.hyperparameters["gru2_dim"] *2).astype('float32') * 10 ** self.hyperparameters["log_layer2_init_scale"]
        h0_bias = np.random.randn(1, self.hyperparameters["gru2_dim"] *2).astype('float32') * 10 ** self.hyperparameters["log_hidden_init_scale"]
        self.h0 = theano.shared(h0, name="h0")
        self.h0_bias = theano.shared(h0_bias, name="h0_bias")

        self.rng = RandomStreams()
        self.padding_size = self.hyperparameters["layer3_window_size"] - 1

        self.train_parameters = [self.all_name_reps,
                                 self.gru_layer1_bias,self.gru_layerrec_bias,# self.gru_layer2_bias,
                                 self.fgruW1,self.fgruW2, self.fgruU1, self.fgruU2, self.fgrub1, self.fgrub2, self.fgruWx, self.fgruUx, self.fgrubx,
                                 self.bgruW1,self.bgruW2, self.bgruU1, self.bgruU2, self.bgrub1, self.bgrub2, self.bgruWx, self.bgruUx, self.bgrubx,
                                 self.fgrurecW1,self.fgrurecW2, self.fgrurecU1, self.fgrurecU2, self.fgrurecb1, self.fgrurecb2, self.fgrurecWx, self.fgrurecUx, self.fgrurecbx,
                                 self.bgrurecW1,self.bgrurecW2, self.bgrurecU1, self.bgrurecU2, self.bgrurecb1, self.bgrurecb2, self.bgrurecWx, self.bgrurecUx, self.bgrurecbx,
                                 self.conv_layer3_copy_code, self.conv_layer3_copy_bias,
                                 self.conv_copy_code, self.conv_copy_bias,self.h0,self.h0_bias,
                                 self.gru_prediction_to_reset, self.gru_prediction_to_hidden, self.gru_prediction_to_update,
                                 self.gru_prev_hidden_to_reset, self.gru_prev_hidden_to_next, self.gru_prev_hidden_to_update,
                                 self.gru_hidden_update_bias, self.gru_update_bias, self.gru_reset_bias,
                                 self.conv_layer3_att_code, self.conv_layer3_att_bias, self.name_bias]

        self.__compile_model_functions()

    def __check_all_hyperparmeters_exist(self):
        all_params = ["D",
                      "log_name_rep_init_scale",
                      "log_layer1_init_scale",
                      "log_layer2_init_scale",
                      "layer3_window_size", "log_layer3_init_scale",
                      "log_copy_init_scale", "log_hidden_init_scale",
                      "log_learning_rate", "momentum", "rmsprop_rho", "dropout_rate", "grad_clip",'gru1_dim']
        for param in all_params:
            assert param in self.hyperparameters, param

    def restore_parameters(self, values):
        for value, param in zip(values, self.train_parameters):
            param.set_value(value)
        self.__compile_model_functions()

    def __get_model_likelihood_for_sentence(self, sentence, graph, name_targets, do_dropout=False, dropout_rate=0.5):

        sentenceT = sentence.T
        if do_dropout:
             conv_weights_code_copy_l3, conv_weights_code_do_copy, conv_weights_code_att_l3, \
                 = dropout_multiple(dropout_rate, self.rng, 
                                    self.conv_layer3_copy_code, self.conv_copy_code, self.conv_layer3_att_code)
             fgruW1,fgruW2, fgruU1, fgruU2, fgrub1, fgrub2, fgruWx, fgruUx, fgrubx, \
                                 bgruW1,bgruW2, bgruU1, bgruU2, bgrub1, bgrub2, bgruWx, bgruUx, bgrubx,\
                                 fgrurecW1,fgrurecW2, fgrurecU1, fgrurecU2, fgrurecb1, fgrurecb2, fgrurecWx, fgrurecUx, fgrurecbx,\
                                 bgrurecW1,bgrurecW2, bgrurecU1, bgrurecU2, bgrurecb1, bgrurecb2, bgrurecWx, bgrurecUx, bgrurecbx\
                                 = dropout_multiple(dropout_rate,self.rng, 
                                             self.fgruW1,self.fgruW2, self.fgruU1, self.fgruU2, self.fgrub1, self.fgrub2, self.fgruWx, self.fgruUx, self.fgrubx,
                                             self.bgruW1,self.bgruW2, self.bgruU1, self.bgruU2, self.bgrub1, self.bgrub2, self.bgruWx, self.bgruUx, self.bgrubx,
                                             self.fgrurecW1,self.fgrurecW2, self.fgrurecU1, self.fgrurecU2, self.fgrurecb1, self.fgrurecb2, self.fgrurecWx, self.fgrurecUx, self.fgrurecbx,
                                             self.bgrurecW1,self.bgrurecW2, self.bgrurecU1, self.bgrurecU2, self.bgrurecb1, self.bgrurecb2, self.bgrurecWx, self.bgrurecUx, self.bgrurecbx)
                                 
             gru_prediction_to_reset, gru_prediction_to_hidden, gru_prediction_to_update, \
                    gru_prev_hidden_to_reset, gru_prev_hidden_to_next, gru_prev_hidden_to_update = \
                    dropout_multiple(dropout_rate, self.rng,
                                 self.gru_prediction_to_reset, self.gru_prediction_to_hidden, self.gru_prediction_to_update,
                                 self.gru_prev_hidden_to_reset, self.gru_prev_hidden_to_next, self.gru_prev_hidden_to_update)

        else:
            conv_weights_code_copy_l3 = self.conv_layer3_copy_code
            conv_weights_code_do_copy = self.conv_copy_code
            conv_weights_code_att_l3 = self.conv_layer3_att_code
            
            fgruW1,fgruW2, fgruU1, fgruU2, fgrub1, fgrub2, fgruWx, fgruUx, fgrubx,\
                                 bgruW1,bgruW2, bgruU1, bgruU2, bgrub1, bgrub2, bgruWx, bgruUx, bgrubx,\
                                 fgrurecW1,fgrurecW2, fgrurecU1, fgrurecU2, fgrurecb1, fgrurecb2, fgrurecWx, fgrurecUx, fgrurecbx,\
                                 bgrurecW1,bgrurecW2, bgrurecU1, bgrurecU2, bgrurecb1, bgrurecb2, bgrurecWx, bgrurecUx, bgrurecbx\
                                 =   self.fgruW1,self.fgruW2, self.fgruU1, self.fgruU2, self.fgrub1, self.fgrub2, self.fgruWx, self.fgruUx, self.fgrubx,\
                                 self.bgruW1,self.bgruW2, self.bgruU1, self.bgruU2, self.bgrub1, self.bgrub2, self.bgruWx, self.bgruUx, self.bgrubx,\
                                 self.fgrurecW1,self.fgrurecW2, self.fgrurecU1, self.fgrurecU2, self.fgrurecb1, self.fgrurecb2, self.fgrurecWx, self.fgrurecUx, self.fgrurecbx,\
                                 self.bgrurecW1,self.bgrurecW2, self.bgrurecU1, self.bgrurecU2, self.bgrurecb1, self.bgrurecb2, self.bgrurecWx, self.bgrurecUx, self.bgrurecbx
                                 
            gru_prediction_to_reset, gru_prediction_to_hidden, gru_prediction_to_update,\
                    gru_prev_hidden_to_reset, gru_prev_hidden_to_next, gru_prev_hidden_to_update = \
                        self.gru_prediction_to_reset, self.gru_prediction_to_hidden, self.gru_prediction_to_update, \
                                 self.gru_prev_hidden_to_reset, self.gru_prev_hidden_to_next, self.gru_prev_hidden_to_update
                                 
        all_name_reps, h0 =  self.all_name_reps, self.h0
        gru_layer1_bias = self.gru_layer1_bias
        gru_layerrec_bias = self.gru_layerrec_bias  

        def _step_Sens1(sen, all_name_reps, fgruW1,fgruW2,fgruU1,fgruU2,fgrub1,fgrub2,fgruWx,fgruUx,fgrubx,\
                                          bgruW1,bgruW2,bgruU1,bgruU2,  bgrub1, bgrub2,  bgruWx,  bgruUx,  bgrubx,\
                                                gru_layer1_bias):
            code_embeddings = all_name_reps[sen]# SentSize x D
            
            proj11 = gru_layer( fgruW1, fgruW2,  fgruU1,  fgruU2,  fgrub1, fgrub2,  fgruWx,  fgruUx,  fgrubx, 
                                                code_embeddings.dimshuffle(0, 1), None, 
                                                prefix='encoder')
            proj12 = gru_layer( bgruW1, bgruW2,  bgruU1,  bgruU2,  bgrub1, bgrub2,  bgruWx,  bgruUx,  bgrubx, 
                                                code_embeddings.dimshuffle(0, 1), None, 
                                                prefix='encoder', mark = True)
            l1_out = T.concatenate([proj11, proj12], axis=1).dimshuffle(0,1) +  gru_layer1_bias.dimshuffle('x', 0)

            return l1_out, code_embeddings
            
        seqs1 = [sentenceT]
        non_seqs1 = [all_name_reps, fgruW1,fgruW2, fgruU1, fgruU2, fgrub1,fgrub2, fgruWx, fgruUx, fgrubx,\
                                    bgruW1,bgruW2, bgruU1, bgruU2, bgrub1,bgrub2, bgruWx, bgruUx, bgrubx, gru_layer1_bias]
        
        [l1_out_list, code_embedding_list], _ = theano.scan(_step_Sens1,
                                    sequences=seqs1,
                                    outputs_info = [None,None],
                                    non_sequences = non_seqs1,
                                    name='Sens_layers1',
                                    n_steps=sentenceT.shape[0],
                                    profile=False,
                                    strict=True)

        # PDG graph
        l1_stmt_vecs =  T.concatenate([T.dot(graph[0],  l1_out_list[:, 0, :]),T.dot(graph[0],  l1_out_list[:, 0, :]),T.dot(graph[0], l1_out_list[:, 0, :])], axis = 1)
        h0 = T.dot(T.mean(l1_stmt_vecs, axis = 0), h0).dimshuffle("x", 0) + self.h0_bias
        
        def _step_Sens2(l1_out, l1_stmt_vec, code_embeddings, 
                                                 fgrurecW1, fgrurecW2,  fgrurecU1,  fgrurecU2,  fgrurecb1, fgrurecb2,  fgrurecWx,  fgrurecUx,  fgrurecbx,\
                                                 bgrurecW1, bgrurecW2,  bgrurecU1,  bgrurecU2,  bgrurecb1, bgrurecb2,  bgrurecWx,  bgrurecUx,  bgrurecbx,\
                                                 gru_layerrec_bias):
            proj21_1 = gru_layer( fgrurecW1, fgrurecW2,  fgrurecU1,  fgrurecU2,  fgrurecb1, fgrurecb2,  fgrurecWx,  fgrurecUx,  fgrurecbx, 
                                                T.concatenate([l1_out, code_embeddings],axis=1), l1_stmt_vec, 
                                                prefix='encoder')
            proj22_1 = gru_layer( bgrurecW1, bgrurecW2,  bgrurecU1,  bgrurecU2,  bgrurecb1, bgrurecb2,  bgrurecWx,  bgrurecUx,  bgrurecbx, 
                                                T.concatenate([l1_out, code_embeddings],axis=1) , l1_stmt_vec, 
                                                prefix='encoder', mark = True)
            l2_out = T.concatenate([proj21_1 ,proj22_1],axis=1).dimshuffle(0,1) +  gru_layerrec_bias.dimshuffle('x', 0)

            return l2_out

        #l2_out = T.switch(l2_out>0, l2_out, 0.1 * l2_out)
    
        seqs2 = [l1_out_list, l1_stmt_vecs, code_embedding_list]
        non_seqs2 = [fgrurecW1,fgrurecW2, fgrurecU1, fgrurecU2, fgrurecb1,fgrurecb2, \
                                                 fgrurecWx, fgrurecUx, fgrurecbx, \
                                                 bgrurecW1,bgrurecW2, bgrurecU1, bgrurecU2, bgrurecb1,bgrurecb2, bgrurecWx, bgrurecUx, bgrurecbx, \
                                                 gru_layerrec_bias]
                                                 
        l2_out_list, _ = theano.scan(_step_Sens2,
                                    sequences=seqs2,
                                    outputs_info = [None],
                                    non_sequences = non_seqs2,
                                    name='Sens_layers2',
                                    n_steps=sentenceT.shape[0],
                                    profile=False,
                                    strict=True)


        l2_out_con2 = l2_out_list.reshape((l2_out_list.shape[0] * l2_out_list.shape[1], l2_out_list.shape[2]))
        padding2 = T.alloc(np.float32(0), (self.hyperparameters["layer3_window_size"]-1)/2, self.D)
        code_embeddings_short = code_embedding_list.reshape((code_embedding_list.shape[0]*code_embedding_list.shape[1], code_embedding_list.shape[2]))
        code_embeddings = T.concatenate([padding2,code_embeddings_short,padding2],axis = 0)
        
        padding = T.alloc(np.float32(0), (self.hyperparameters["layer3_window_size"]-1)/2, self.hyperparameters["gru2_dim"] * 2)
        l2_out_con = T.concatenate([padding, l2_out_con2, padding], axis = 0)
        l2_out = l2_out_con.dimshuffle("x", 1, 0, "x") 

        def step(target_token_id, hidden_state, attention_features,
                 gru_prediction_to_reset, gru_prediction_to_hidden, gru_prediction_to_update,
                 gru_prev_hidden_to_reset, gru_prev_hidden_to_next, gru_prev_hidden_to_update,
                 gru_hidden_update_bias, gru_update_bias, gru_reset_bias,
                 conv_att_weights_code_l3, conv_att_layer3_bias,
                 conv_weights_code_copy_l3, conv_layer3_copy_bias,
                 conv_weights_code_do_copy, conv_copy_bias,
                 code_embeddings, all_name_reps, use_prev_stat):
                 
            gated_l2 = attention_features * T.switch(hidden_state>0, hidden_state, 0.01 * hidden_state).dimshuffle(0, 1, 'x', 'x')
            gated_l2 = gated_l2 / gated_l2.norm(2)
            # Normal Attention
            code_convolved_l3 = T.nnet.conv2d(gated_l2, conv_att_weights_code_l3,
                                              image_shape=(1, self.hyperparameters["gru2_dim"] *2, None, 1),
                                              filter_shape=self.conv_layer3_att_code.get_value().shape)[:, 0, :, 0]

            l3_out = code_convolved_l3 + conv_att_layer3_bias
            code_toks_weights = T.nnet.softmax(l3_out)  # This should be one dimension (the size of the sentence)
            predicted_embedding = T.tensordot(code_toks_weights, code_embeddings[(self.hyperparameters["layer3_window_size"]-1)/2:-((self.hyperparameters["layer3_window_size"]-1)/2)], [[1], [0]])[0]


            # Copy Attention
            code_copy_convolved_l3 = T.nnet.conv2d(gated_l2, conv_weights_code_copy_l3,
                                          image_shape=(1, self.hyperparameters["gru2_dim"] *2, None, 1),
                                          filter_shape=self.conv_layer3_copy_code.get_value().shape)[:, 0, :, 0]

            copy_l3_out = code_copy_convolved_l3 + conv_layer3_copy_bias
            copy_pos_probs = T.nnet.softmax(copy_l3_out)[0]  # This should be one dimension (the size of the sentence)

            # Do we copy?
            do_copy_code = T.max(T.nnet.conv2d(gated_l2, conv_weights_code_do_copy,
                                              image_shape=(1, self.hyperparameters["gru2_dim"] *2, None, 1),
                                              filter_shape=self.conv_copy_code.get_value().shape)[:, 0, :, 0])
            copy_prob = T.nnet.sigmoid(do_copy_code + conv_copy_bias)

            # Get the next hidden!
            if do_dropout:
                # For regularization, we can use the context embeddings *some* of the time
                embedding_used = T.switch(use_prev_stat, all_name_reps[target_token_id], predicted_embedding)
            else:
                embedding_used = all_name_reps[target_token_id]

            reset_gate = T.nnet.sigmoid(
                T.dot(embedding_used, gru_prediction_to_reset) + T.dot(hidden_state, gru_prev_hidden_to_reset) + gru_reset_bias
            )
            update_gate = T.nnet.sigmoid(
                T.dot(embedding_used, gru_prediction_to_update) + T.dot(hidden_state, gru_prev_hidden_to_update) + gru_update_bias
            )
            hidden_update = T.tanh(
                T.dot(embedding_used, gru_prediction_to_hidden) + reset_gate * T.dot(hidden_state, gru_prev_hidden_to_next) + gru_hidden_update_bias
            )

            next_hidden = (1. - update_gate) * hidden_state + update_gate * hidden_update

            return next_hidden, predicted_embedding, copy_pos_probs, copy_prob, code_toks_weights, gated_l2,attention_features


        use_prev_stat = self.rng.binomial(n=1, p=1. - dropout_rate)
        non_sequences = [l2_out,
                         gru_prediction_to_reset, gru_prediction_to_hidden, gru_prediction_to_update, # GRU
                         gru_prev_hidden_to_reset, gru_prev_hidden_to_next, gru_prev_hidden_to_update,
                         self.gru_hidden_update_bias, self.gru_update_bias, self.gru_reset_bias,
                         conv_weights_code_att_l3, self.conv_layer3_att_bias,  # Normal Attention
                         conv_weights_code_copy_l3, self.conv_layer3_copy_bias, # Copy Attention
                         conv_weights_code_do_copy, self.conv_copy_bias, # Do we copy?
                         code_embeddings, self.all_name_reps, use_prev_stat]

        [h, predictions, copy_weights, copy_probs, attention_weights, filtered_features,attenfeature], _ = \
                                                                           theano.scan(step, sequences=name_targets,
                                                                           outputs_info=[h0, None, None, None, None, None, None],
                                                                           name="target_name_scan",
                                                                           non_sequences=non_sequences, strict=True)

        name_log_probs = log_softmax(T.dot(predictions, T.transpose(self.all_name_reps[:-1])) + self.name_bias) # SxD, DxK -> SxK


        return sentence, name_targets, copy_weights, attention_weights, copy_probs, name_log_probs, filtered_features,l1_stmt_vecs, sentenceT,

    def model_objective(self, copy_probs, copy_weights, is_copy_matrix, name_log_probs, name_targets, targets_is_unk):
        use_copy_prob = T.switch(T.sum( T.cast(is_copy_matrix,theano.config.floatX), axis=1) > 0, T.log(copy_probs) + T.log(T.sum(T.mul( T.cast(is_copy_matrix,theano.config.floatX) , copy_weights), axis=1,dtype=theano.config.floatX)+np.float32(10e-8)), np.float32(-1000.))
        use_model_prob = T.switch(targets_is_unk, np.float32(-10), np.float32(0)) + T.log(np.float32(1) - copy_probs) + name_log_probs[T.arange(name_targets.shape[0]), name_targets]
        correct_answer_log_prob = logsumexp(use_copy_prob, use_model_prob)
        return T.mean(correct_answer_log_prob)

    def __compile_model_functions(self):
            grad_acc = [theano.shared(np.zeros(param.get_value().shape, dtype=np.float32)) for param in self.train_parameters] \
                        + [theano.shared(np.float32(0), name="sentence_count")]
            sentence = T.imatrix("sentence")
            graph = T.tensor3("graph")
            is_copy_matrix = T.imatrix("is_copy_matrix")
            name_targets = T.ivector("name_targets")
            targets_is_unk = T.ivector("targets_is_unk")

            sentence.tag.test_value = np.asarray([np.arange(105).astype(np.int32)]).T

            graph.tag.test_value = np.ones((3,1,1)).astype(np.float32)
            
            name_targets.tag.test_value = np.arange(5).astype(np.int32)
            targets_is_unk.tag.test_value = np.array([0, 0, 1, 0, 0], dtype=np.int32)
            is_copy_test_value = [[i % k == k-2 for i in xrange(105 - self.padding_size)] for k in [1, 7, 10, 25, 1]]
            is_copy_matrix.tag.test_value = np.array(is_copy_test_value, dtype=np.int32)
            

            _, _, copy_weights, _, copy_probs, name_log_probs, _,_, _\
                    = self.__get_model_likelihood_for_sentence(sentence, graph, name_targets, do_dropout=True,
                                                           dropout_rate=self.hyperparameters["dropout_rate"])

            correct_answer_log_prob = self.model_objective(copy_probs[:-1], copy_weights[:-1], is_copy_matrix[1:], name_log_probs[:-1],
                                                           name_targets[1:], targets_is_unk[1:])

            grad = T.grad(correct_answer_log_prob, self.train_parameters)

            self.grad_accumulate = theano.function(inputs=[sentence, graph, is_copy_matrix, targets_is_unk, name_targets],
                                                   updates=[(v, v+g) for v, g in zip(grad_acc, grad)] + [(grad_acc[-1], grad_acc[-1]+1)],
                                                   #mode='NanGuardMode'
                                                   )

            normalized_grads = [T.switch(grad_acc[-1] >0, g / grad_acc[-1], g) for g in grad_acc[:-1]]

                
            step_updates, ratios = nesterov_rmsprop_multiple(self.train_parameters, normalized_grads,
                                                    learning_rate=10 ** self.hyperparameters["log_learning_rate"],
                                                    rho=self.hyperparameters["rmsprop_rho"],
                                                    momentum=self.hyperparameters["momentum"],
                                                    grad_clip=self.hyperparameters["grad_clip"],
                                                    output_ratios=True)
                                    
            step_updates.extend([(v, T.zeros(v.shape, dtype =  theano.config.floatX)) for v in grad_acc[:-1]])  # Set accumulators to 0
            
            step_updates.append((grad_acc[-1], np.float32(0)))

            self.grad_step = theano.function(inputs=[], updates=step_updates, outputs=ratios)


            test_graph = T.tensor3("test_graph")
            test_sentence, test_name_targets, test_copy_weights, test_attention_weights, test_copy_probs, test_name_log_probs,\
                             test_attention_features,_,_ \
                = self.__get_model_likelihood_for_sentence(T.imatrix("test_sentence") , test_graph,  T.ivector("test_name_targets"),
                                                          do_dropout=False)

            self.copy_probs = theano.function(inputs=[test_name_targets, test_sentence,test_graph], 
                                                      outputs=[test_copy_weights, test_copy_probs, test_name_log_probs])


            test_copy_matrix = T.imatrix("test_copy_matrix")
            test_target_is_unk = T.ivector("test_target_is_unk")
            ll = self.model_objective(test_copy_probs[:-1], test_copy_weights[:-1], test_copy_matrix[1:], test_name_log_probs[:-1],
                                                           test_name_targets[1:], test_target_is_unk[1:])
            self.copy_logprob = theano.function(inputs=[test_sentence,test_graph, test_copy_matrix, test_target_is_unk, test_name_targets],
                                                outputs=ll)
            self.attention_weights = theano.function(inputs=[test_name_targets, test_sentence, test_graph],
                                                     outputs=test_attention_weights)
            layer3_padding = self.hyperparameters["layer3_window_size"] - 1
            upper_pos = -layer3_padding/2 + 1 if -layer3_padding/2 +1 < 0 else None

            self.attention_features = theano.function(inputs=[test_sentence, test_graph, test_name_targets],
                                                      outputs=test_attention_features[-1, 0, :, layer3_padding/2+1:upper_pos, 0])


    def log_prob_with_targets(self, sentence, graph, copy_matrices, targets_is_unk, name_targets):
        ll, count = 0, 0
        for i in xrange(len(name_targets)):
            shapek = graph[i].shape[1]
            graphi = np.array([graph[i][:shapek,:], graph[i][shapek:2*shapek,:], graph[i][2*shapek:3*shapek,:]]).astype(np.float32)
            max_len = max([item.shape[0] for item in sentence[i]])
            batch_code_sentencesk = np.array([np.array([item for item in code_sen] + \
                                        [self.naming_data.all_tokens_dictionary.get_id_or_unk(self.NONE) \
                                        for ii in range(max_len - code_sen.shape[0])], dtype=np.int32) for code_sen in sentence[i]])
            if batch_code_sentencesk.shape[1] != 1: batch_code_sentencesk = batch_code_sentencesk.T
            if batch_code_sentencesk.shape[1] != graphi.shape[1]: batch_code_sentencesk = batch_code_sentencesk.T
            max_len_copy = max([item.shape[0] for item in copy_matrices[i]])
            copy_matricesk = np.array([np.array([item for item in code_sen] + \
                                        [0 for ii in range(max_len_copy - code_sen.shape[0])], dtype=np.int32) for code_sen in copy_matrices[i]])
            count += 1
            ll += self.copy_logprob(batch_code_sentencesk, graphi, copy_matricesk, \
                                        np.array(targets_is_unk[i], dtype=np.int32), np.array(name_targets[i], dtype=np.int32))
        return (ll /count)

